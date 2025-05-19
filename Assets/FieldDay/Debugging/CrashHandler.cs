#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using UnityEngine;
using EasyBugReporter;
using TMPro;
using UnityEngine.UI;
using BeauUtil.Debugger;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.IL2CPP.CompilerServices;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.EventSystems;
#endif // UNITY_EDITOR

namespace FieldDay.Debugging {

    /// <summary>
    /// Crash handling API.
    /// </summary>
    public class CrashHandler : MonoBehaviour {
        public Canvas Canvas;
        public TMP_Text ProgressText;
        public TMP_Text ExceptionText;
        public Button DumpButton;

        [NonSerialized] private int m_DumpCounter;

        private void Awake() {
            DumpButton.onClick.AddListener(Dump);
        }

        private void LateUpdate() {
            if (m_DumpCounter > 0 && --m_DumpCounter == 0) {
                Canvas.enabled = true;
            }

            if (m_DumpCounter == 0 && (Input.GetKeyDown(KeyCode.Return))) {
                Dump();
            }
        }

        private void Dump() {
            BugReporter.DumpContext();
            Canvas.enabled = false;
            m_DumpCounter = 3;
        }

        private void Populate(string exception, string context) {
            ProgressText.SetText(context);
            ExceptionText.SetText(exception);
        }

        #region Static API

        static public bool Enabled = false;

        public delegate void OnCrashDelegate(Exception exception, string error);
        public delegate void CrashDisplayDelegate(Exception exception, string error, out string outContext);

        static private bool s_Registered;
        static private CrashHandler s_Instance;

        static public event OnCrashDelegate OnCrash;
        static public event CrashDisplayDelegate DisplayCrash;

        static public void Register() {
            if (s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
            Application.logMessageReceived += OnApplicationLog;
            s_Registered = true;

            DumpSourceCollection src = new DumpSourceCollection();
            src.Add(new ScreenshotContext());
            src.Add(new LogContext(EasyBugReporter.LogTypeMask.Development | EasyBugReporter.LogTypeMask.Log));
            src.Add(new UnityContext());
            src.Add(new SystemInfoContext());
            BugReporter.DefaultSources = src;
        }

        static public void Deregister() {
            if (!s_Registered) {
                return;
            }

            AppDomain.CurrentDomain.DomainUnload -= OnDomainUnload;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            Application.logMessageReceived -= OnApplicationLog;
            s_Registered = false;
        }

        static private void OnDomainUnload(object sender, EventArgs e) {
            Deregister();
        }

        static private void OnApplicationLog(string condition, string stackTrace, LogType type) {
            if (type != LogType.Exception) {
                return;
            }

            //Console.WriteLine("log exception");
            OnExceptionEncountered(string.Join('\n', condition, stackTrace), null);
        }

        static private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e) {
            //Console.WriteLine("unhandled exception");
            OnExceptionEncountered(null, e.ExceptionObject as Exception);
        }

        static private void OnExceptionEncountered(string exceptionInfo, Exception exception) {
            if (!Enabled) {
                return;
            }

#if UNITY_EDITOR
            //if (EditorApplication.isCompiling || EditorApplication.isPaused || EditorApplication.isPlayingOrWillChangePlaymode) {
            //    return;
            //}
#endif // UNITY_EDITOR

            Cursor.visible = true;

            if (!s_Instance) {
                string context = null;
                OnCrash?.Invoke(exception, exceptionInfo);
                DisplayCrash?.Invoke(exception, exceptionInfo, out context);

                CrashHandler prefab = UnityEngine.Resources.Load<CrashHandler>("CrashHandler");
                if (prefab != null) {
                    s_Instance = Instantiate(prefab);
                    s_Instance.Populate(exception?.Message ?? exceptionInfo, context);
                } else {
                    Debug.LogErrorFormat("[CrashHandler] No 'CrashHandler' prefab to instantiate");
                    Debug.LogFormat(exception?.Message ?? exceptionInfo);
                    Debug.LogFormat(context);
                    BugReporter.DumpContext();
                }
            }
        }

        #endregion // Static API

#if DEVELOPMENT

        [EngineMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo sysMenu = new DMInfo("Crash");
            sysMenu.AddToggle("Enable Crash Handler", () => Enabled, (b) => Enabled = b);
            sysMenu.AddButton("Generate Dump", () => BugReporter.DumpContext());
            sysMenu.AddDivider();
            sysMenu.AddButton("Crash (NullRef)", CrashNullRef);
            sysMenu.AddButton("Crash (NullPtr)", CrashNullPtr);
            sysMenu.AddButton("Crash (SegFault)", CrashSegFault);
            sysMenu.AddButton("Crash (Assert)", CrashAssertion);
            sysMenu.AddButton("Crash (StackOverflow)", CrashStackOverflow);
            sysMenu.AddButton("Hang (MutexDeadlock)", HangMutexDeadlock, ExcludeEditor);
            sysMenu.AddButton("Hang (InfiniteLoop)", HangInfiniteLoop, ExcludeEditor);
            return sysMenu;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static private void CrashNullRef() {
            IEventDispatcher dispatcher = null;
            dispatcher.Queue("dummy");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static private unsafe void CrashNullPtr() {
            Vector3* v = null;
            float y = v->y;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static private unsafe void CrashSegFault() {
            int* ptr = null;
            ptr[0] = 5;
            ptr[1] = 8;
            ptr[2] = ptr[0];
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static private unsafe void CrashAssertion() {
            Assert.True(false, "Crash menu created this assertion");
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static private unsafe void CrashStackOverflow() {
            RawStateBlock64 randomState = default;
            randomState.Store<Vector3>(UnityEngine.Random.insideUnitSphere);
            CrashStackOverflow();
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static private unsafe void HangMutexDeadlock() {
            object lockObj = new object();
            Monitor.Enter(lockObj);
            Monitor.Wait(lockObj);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        [Il2CppSetOption(Option.NullChecks, false)]
        static private unsafe void HangInfiniteLoop() {
            int x = 0;
            while (true) {
                x++;
            }
        }

        static private readonly DMPredicate ExcludeEditor =
#if UNITY_EDITOR
            () => {
                return false;
            };
#else
            null;
#endif // UNITY_EDITOR

#endif // DEVELOPMENT
        }
}