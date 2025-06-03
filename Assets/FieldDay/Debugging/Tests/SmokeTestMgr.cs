#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

#define FIELD_DAY_TESTS

using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace FieldDay.Debugging {
    static public class SmokeTestMgr {
#if FIELD_DAY_TESTS
        private const int MaxTests = 64;

        private enum SmokeTestState {
            Uninitialized,
            Queued,
            Running,
            Success,
            TimedOut,
            EncounteredError,
            EncounteredException,
            EncounteredAssert
        }

        private sealed class Context : ISmokeTestContext, IDisposable {
            public void AttachScreenshot(Texture2D screenshot) {

            }

            public void AttachScreenshot(RenderTexture screenshot) {

            }

            public IEnumerator LoadMainScene(string scenePath) {
                Game.Scenes.LoadMainScene(scenePath, true);
                while(Game.Scenes.IsMainLoading()) {
                    yield return null;
                }
            }

            public IEnumerator LoadMainScene(SceneReference sceneReference) {
                Game.Scenes.LoadMainScene(sceneReference, true);
                while (Game.Scenes.IsMainLoading()) {
                    yield return null;
                }
            }

            public void Dispose() {

            }
        }

        static private readonly string[] CachedSmokeTestStateStrings = ReflectionCache.EnumInfo<SmokeTestState>().InspectorNames;

        static private Action s_Reset;
        static private readonly RingBuffer<SmokeTestData> s_ScheduledTests = new RingBuffer<SmokeTestData>(MaxTests, RingBufferMode.Fixed);
        static private Context s_CurrentTestContext;
        static private Routine s_CurrentTestRoutine;
        static private SmokeTestState s_TestState;
        static private float s_TimeOutAccumulator;
        static private readonly StringBuilder s_LogAccumulator = new StringBuilder(4096);
        static private readonly StringBuilder s_DebugBuilder = new StringBuilder(1024);

        static private bool s_CrashHandlerRestoreState;
        static private bool s_DebugDrawRestoreState;

        static private readonly RingBuffer<SmokeTestData> s_NamedSmokeTests = new RingBuffer<SmokeTestData>(MaxTests, RingBufferMode.Fixed);

        static private void BeginQueue() {
            s_TestState = SmokeTestState.Queued;
            s_LogAccumulator.Length = 0;
            s_CurrentTestRoutine.Stop();

            Time.timeScale = 1;

            DebugInput.Pause();
            DebugFlags.SetAutomatedTestActive(true);

            s_DebugDrawRestoreState = DebugDraw.IsRenderingEnabled();
            DebugDraw.EnableRendering();

            s_CrashHandlerRestoreState = CrashHandler.Enabled;
            CrashHandler.Enabled = false;

            Application.logMessageReceived -= OnApplicationLog;
            Application.logMessageReceived += OnApplicationLog;

            Assert.DeregisterLogHook();
            Assert.SetFailureMode(Assert.FailureMode.Automatic);

            GameLoop.OnDebugUpdate.Register(Tick);
        }

        static private void EndQueue() {
            Assert.SetFailureMode(Assert.FailureMode.User);
            Assert.RegisterLogHook();

            Application.logMessageReceived -= OnApplicationLog;

            DebugFlags.SetAutomatedTestActive(false);
            DebugInput.Resume();

            CrashHandler.Enabled = s_CrashHandlerRestoreState;

            if (!s_DebugDrawRestoreState) {
                DebugDraw.DisableRendering();
            }

            GameLoop.OnDebugUpdate.Deregister(Tick);

            s_TestState = SmokeTestState.Uninitialized;
            s_LogAccumulator.Length = 0;
            s_CurrentTestRoutine.Stop();
        }

        static private void Tick(float deltaTime) {
            if (!s_ScheduledTests.TryPeekFront(out SmokeTestData test)) {
                EndQueue();
                return;
            }

            if (s_TestState == SmokeTestState.Queued) {
                if (!TryReset()) {
                    UnityEngine.Debug.LogError("Exception encountered when attempting to reset state. Check prior logs for details. Please fix.");
                    s_ScheduledTests.Clear();
                    return;
                }

                s_TestState = SmokeTestState.Running;
                s_CurrentTestContext = new Context();

                BeginTest(test);
                if (s_TestState == SmokeTestState.Running) {
                    test.Execute?.Invoke(s_CurrentTestContext);
                }
                if (s_TestState == SmokeTestState.Running) {
                    if (test.ExecuteAsync != null) {
                        s_CurrentTestRoutine.Replace(test.ExecuteAsync(s_CurrentTestContext)).SetPriority(1000000);
                    }
                }
            } else if (s_TestState == SmokeTestState.Running) {
                if (s_CurrentTestRoutine) {
                    s_TimeOutAccumulator += deltaTime;
                    if (s_TimeOutAccumulator > test.TimeOut) {
                        FailTest(SmokeTestState.TimedOut);
                    }
                } else {
                    s_TestState = SmokeTestState.Success;
                }
            } else {
                EndTest(test);
                SmokeTestState finalState = s_TestState;
                s_ScheduledTests.PopFront();
                s_TestState = SmokeTestState.Queued;

                ReportTestResults(test, finalState, s_CurrentTestContext);
                s_CurrentTestContext.Dispose();
                s_CurrentTestContext = null;
            }

            if (s_TestState >= SmokeTestState.Running) {
                s_DebugBuilder.Append("CURRENT TEST: ").Append(test.Name)
                    .Append("\nSTATE: ").Append(CachedSmokeTestStateStrings[(int) s_TestState]);
                DebugDraw.AddViewportText(new Vector2(0.5f, 0), new Vector2(0, 16), s_DebugBuilder, Color.black, 0, TextAnchor.LowerCenter, DebugTextStyle.BackgroundDarkOpaque);
                s_DebugBuilder.Clear();
            }
        }

        static private void FailTest(SmokeTestState state) {
            s_CurrentTestRoutine.Stop();
            if (s_TestState < state) {
                s_TestState = state;
            }
        }

        static private bool TryReset() {
            try {
                Time.timeScale = 1;
                s_TimeOutAccumulator = 0;
                s_LogAccumulator.Length = 0;

                Sfx.StopAll();

                if (s_Reset != null) {
                    s_Reset();
                }
                return true;
            } catch(Exception e) {
                UnityEngine.Debug.LogException(e);
                return false;
            }
        }

        static private void BeginTest(in SmokeTestData test) {
            try {
                test.Prolog?.Invoke(s_CurrentTestContext);
            }
            catch(Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        static private void EndTest(in SmokeTestData test) {
            try {
                test.Epilog?.Invoke(s_CurrentTestContext);
            } catch (Exception e) {
                UnityEngine.Debug.LogException(e);
            }
        }

        static private void ReportTestResults(in SmokeTestData test, SmokeTestState stateWhenFinished, Context context) {
            // TODO: Implement
        }

        #region Handlers

        static private void OnApplicationLog(string condition, string stackTrace, UnityEngine.LogType type) {
            if (s_TestState < SmokeTestState.Running) {
                return;
            }

            Report(s_LogAccumulator, condition, stackTrace, type);
            switch(type) {
                case LogType.Error: {
                    FailTest(SmokeTestState.EncounteredError);
                    break;
                }
                case LogType.Exception: {
                    FailTest(SmokeTestState.EncounteredException);
                    break;
                }
                case LogType.Assert: {
                    FailTest(SmokeTestState.EncounteredAssert);
                    break;
                }
            }
        }

        static private void Report(StringBuilder sb, string condition, string stackTrace, UnityEngine.LogType type) {
            switch (type) {
                case LogType.Assert: {
                        sb.Append("ASSERT: ");
                        break;
                    }

                case LogType.Error: {
                        sb.Append("ERROR: ");
                        break;
                    }

                case LogType.Exception: {
                        sb.Append("EXCEPTION: ");
                        break;
                    }

                case LogType.Warning: {
                        sb.Append("WARN: ");
                        break;
                    }
            }
            sb.Append(condition).Append('\n').Append(stackTrace).Append('\n');
        }

        #endregion // Handlers

#endif // FIELD_DAY_TESTS

        #region Public Api

        [Conditional("FIELD_DAY_TESTS")]
        static public void RegisterResetHandler(Action handler) {
#if FIELD_DAY_TESTS
            s_Reset += handler;
#endif // FIELD_DAY_TESTS
        }

        [Conditional("FIELD_DAY_TESTS")]
        static public void DeregisterResetHandler(Action handler) {
#if FIELD_DAY_TESTS
            s_Reset -= handler;
#endif // FIELD_DAY_TESTS
        }

        /// <summary>
        /// Schedules a test to execute.
        /// </summary>
        [Conditional("FIELD_DAY_TESTS")]
        static public void ScheduleTest(in SmokeTestData testData) {
#if FIELD_DAY_TESTS
            if (s_TestState == SmokeTestState.Uninitialized) {
                BeginQueue();
            }
            s_ScheduledTests.PushBack(testData);
#endif // FIELD_DAY_TESTS
        }

        /// <summary>
        /// Schedules a test to execute.
        /// </summary>
        [Conditional("FIELD_DAY_TESTS")]
        static public void ScheduleTest(string testName) {
#if FIELD_DAY_TESTS
            Assert.NotNull(testName);
            int existingTestIdx = s_NamedSmokeTests.FindIndex((a, b) => a.Name.Equals(b, StringComparison.Ordinal), testName);
            Assert.True(existingTestIdx >= 0, "Smoke Test with name '{0}' not registered", testName);
            ScheduleTest(s_NamedSmokeTests[existingTestIdx]);
#endif // FIELD_DAY_TESTS
        }

        /// <summary>
        /// Registers the given smoke test, to be later referenced by name.
        /// </summary>
        [Conditional("FIELD_DAY_TESTS")]
        static public void RegisterTest(in SmokeTestData testData) {
#if FIELD_DAY_TESTS
            Assert.NotNull(testData.Name);
            int existingTestIdx = s_NamedSmokeTests.FindIndex((a, b) => a.Name.Equals(b, StringComparison.Ordinal), testData.Name);
            Assert.True(existingTestIdx < 0, "Smoke Test with name '{0}' already registered", testData.Name);
            s_NamedSmokeTests.PushBack(testData);
#endif // FIELD_DAY_TESTS
        }

        #endregion // Public Api

        #region Menu

#if DEVELOPMENT && FIELD_DAY_TESTS

        [EngineMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Smoke Tests", 64);
            foreach(var testRegistration in Reflect.FindMethods<SmokeTestProviderAttribute>(ReflectionCache.UserAssemblies, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.NonPublic, false)) {
                MethodInfo m = testRegistration.Info;
                if (m.ReturnParameter.ParameterType != typeof(void) || m.GetParameters().Length != 0) {
                    UnityEngine.Debug.LogErrorFormat("[SmokeTestMgr] Method '{0}::{1}' does not match required signature of 'void func()'", m.DeclaringType.FullName, m.Name);
                } else {
                    m.Invoke(null, Array.Empty<object>());
                }
            }

            return menu;
        }

#endif // DEVELOPMENT && FIELD_DAY_TESTS

        #endregion // Menu
    }

    /// <summary>
    /// Smoke test data.
    /// </summary>
    public struct SmokeTestData {
        public string Name;
        public Action<ISmokeTestContext> Prolog;
        public Action<ISmokeTestContext> Execute;
        public Func<ISmokeTestContext, IEnumerator> ExecuteAsync;
        public Action<ISmokeTestContext> Epilog;
        public float TimeOut;
    }

    /// <summary>
    /// Interface for a smoke test context.
    /// </summary>
    public interface ISmokeTestContext {
        void AttachScreenshot(Texture2D texture);
        void AttachScreenshot(RenderTexture texture);

        IEnumerator LoadMainScene(string scenePath);
        IEnumerator LoadMainScene(SceneReference sceneReference);
    }

    /// <summary>
    /// Method called to register smoke tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SmokeTestProviderAttribute : Attribute {
    }

    //public struct SmokeTestReport {
    //    public string 
    //}
}