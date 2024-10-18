#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Diagnostics;
using System.Reflection;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.UI;
using EasyBugReporter;
using FieldDay.HID;
using FieldDay.HID.XR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Scripting;

#if UNITY_EDITOR
#endif // UNITY_EDITOR

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug console.
    /// </summary>
    [DefaultExecutionOrder(-9999)]
    public sealed class DebugConsole : MonoBehaviour {
        #region Events

        /// <summary>
        /// Invoked when time scale is updated.
        /// </summary>
        static public readonly CastableEvent<bool> OnPauseUpdated = new CastableEvent<bool>();

        /// <summary>
        /// Invoked when time scale is updated.
        /// </summary>
        static public readonly CastableEvent<float> OnTimeScaleUpdated = new CastableEvent<float>();

        #endregion // Events

#if DEVELOPMENT

        private enum StepState {
            Uninitialized,
            Queued,
            Executing
        }

        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleKey = KeyCode.BackQuote;
        [SerializeField] private CanvasGroup m_MinimalGroup = null;
        [SerializeField] private ConsoleTimeDisplay m_TimeDisplay = null;
        [SerializeField] private RaycastZone m_InputBlocker = null;

        [Header("Debug Menu")]
        [SerializeField] private DMMenuUI m_DebugMenus = null;
        [SerializeField] private CanvasGroup m_DebugMenuInput = null;

        [Header("Quick Menu")]
        [SerializeField] private DMMenuUI m_QuickMenu = null;
        [SerializeField] private CanvasGroup m_QuickMenuInput = null;

        #endregion // Inspector

        [NonSerialized] private float m_TimeScale = 1;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private bool m_MinimalVisible;
        [NonSerialized] private bool m_VisibilityWhenDebugMenuOpened;
        [NonSerialized] private bool m_CursorWhenDebugMenuOpened;
        [NonSerialized] private bool m_MenuOpen;
        [NonSerialized] private bool m_MenuUIInitialized;
        [NonSerialized] private StepState m_SingleStepState;

        static private DMInfo s_RootMenu;
        static private DMInfo s_QuickMenu;

        private void Awake() {
            GameLoop.OnDebugUpdate.Register(OnPreUpdate);
            GameLoop.QueuePreUpdate(LoadMenu);
        }

        private void Start() {
            m_DebugMenus.gameObject.SetActive(false);
            m_Canvas.enabled = false;
            m_MinimalGroup.blocksRaycasts = false;

#if UNITY_2022_2_OR_NEWER
            UnityEngine.Debug.developerConsoleEnabled = false;
#endif // UNITY_2022_2_OR_NEWER
        }

        private void OnDestroy() {
            GameLoop.OnDebugUpdate.Deregister(OnPreUpdate);
        }

        private void OnPreUpdate() {
            if (!enabled) {
                return;
            }

            CheckKeyboardShortcuts();
            CheckTimeInput();
            UpdateMinimalLayer();
            UpdateMenu();

#if !UNITY_EDITOR
            UnityEngine.Debug.ClearDeveloperConsole();
            UnityEngine.Debug.developerConsoleVisible = false;
#endif // !UNITY_EDITOR
        }

        #region Keyboard Shortcuts

        private void CheckKeyboardShortcuts() {
            if (DebugInput.IsPressed(InputModifierKeys.CtrlShift, KeyCode.F9)
                || DebugInput.IsPressed(InputModifierKeys.CtrlShift, KeyCode.Backspace)
                || DebugInput.IsPressed(InputModifierKeys.R1 | InputModifierKeys.R2, XRHandIndex.Right, XRHandButtons.Menu)) {
                BugReporter.DumpContext();
            }
        }

        #endregion // Keyboard Shortcuts

        #region Time Scale

        private void CheckTimeInput() {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                if (Input.GetKeyDown(KeyCode.Minus)) {
                    UpdateTimescale(m_TimeScale / 2);
                } else if (Input.GetKeyDown(KeyCode.Equals)) {
                    if (m_TimeScale * 2 < 100) {
                        UpdateTimescale(m_TimeScale * 2);
                    }
                } else if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    UpdateTimescale(1);
                }
            }
        }

        private void UpdateTimescale(float timeScale) {
            m_TimeScale = timeScale;
            if (!m_Paused) {
                Time.timeScale = timeScale;
                OnTimeScaleUpdated.Invoke(timeScale);
            }

            m_TimeDisplay.UpdateTimescale(m_TimeScale);
        }

        private void SetPaused(bool paused) {
            if (m_Paused == paused) {
                return;
            }

            m_Paused = paused;
            Routine.Settings.Paused = paused;
            OnPauseUpdated.Invoke(paused);
            GameLoop.SetDebugPause(paused);
            m_InputBlocker.enabled = paused;
            AudioListener.pause = paused;

            if (paused) {
                Time.timeScale = 0;
                m_TimeDisplay.UpdateState(true);
                OnTimeScaleUpdated.Invoke(0);
                EventSystem.current?.SetSelectedGameObject(null);
            } else {
                Time.timeScale = m_TimeScale;
                m_TimeDisplay.UpdateState(false);
                OnTimeScaleUpdated.Invoke(m_TimeScale);
            }
        }

        #endregion // Time Scale

        #region Menu

        private void UpdateMenu() {

            bool canHaveMenuOpen = !Game.Scenes.IsMainLoading();

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W) && canHaveMenuOpen) {
                SetMenuVisible(!m_MenuOpen);
                Game.Input.ConsumeAllInputForFrame();
            }

            if (m_DebugMenus.isActiveAndEnabled) {
                if (!canHaveMenuOpen) {
                    SetMenuVisible(false);
                } else {
                    m_DebugMenus.UpdateElements();

                    if (DebugInput.IsPressed(DebugInputButtons.Cancel)) {
                        m_DebugMenus.TryPopMenu();
                    } else if (DebugInput.IsPressed(DebugInputButtons.DPadLeft)) {
                        m_DebugMenus.TryPreviousPage();
                    } else if (DebugInput.IsPressed(DebugInputButtons.DPadRight)) {
                        m_DebugMenus.TryNextPage();
                    }
                }
            }
        }

        static private void LoadMenu() {
            LoadRootMenu();
            LoadQuickMenu();
        }

        static private void LoadRootMenu() {
            s_RootMenu = new DMInfo("Debug", 16);

            // load menus from user assemblies
            foreach (var pair in Reflect.FindMethods<DebugMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                if (pair.Info.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                if (pair.Info.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) pair.Info.Invoke(null, Array.Empty<object>());

                if (menu != null) {
                    DMInfo.MergeSubmenu(s_RootMenu, menu, true);
                }
            }

            // load engine menus from user assemblies
            DMInfo engineMenu = new DMInfo("Engine", 16);
            foreach (var pair in Reflect.FindMethods<EngineMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                if (pair.Info.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                if (pair.Info.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) pair.Info.Invoke(null, Array.Empty<object>());

                if (menu != null) {
                    DMInfo.MergeSubmenu(engineMenu, menu, true);
                }
            }

            DMInfo.SortByLabel(engineMenu);

            DMInfo.MergeSubmenu(s_RootMenu, engineMenu, false);
            DMInfo.SortByLabel(s_RootMenu);
        }

        static private void LoadQuickMenu() {
            s_QuickMenu = new DMInfo("Quick", 16);

            // load menus from user assemblies
            foreach (var pair in Reflect.FindMethods<QuickMenuFactoryAttribute>(ReflectionCache.UserAssemblies, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly)) {
                if (pair.Info.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.FullName, pair.Info.Name);
                    continue;
                }

                if (pair.Info.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.FullName, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) pair.Info.Invoke(null, Array.Empty<object>());

                if (menu != null) {
                    DMInfo.MergeSubmenu(s_QuickMenu, menu);
                }
            }

            DMInfo.SortByLabel(s_QuickMenu);
        }

        private void SetMenuVisible(bool visible) {
            if (m_MenuOpen == visible) {
                return;
            }

            m_MenuOpen = visible;
            if (visible) {
                m_VisibilityWhenDebugMenuOpened = m_MinimalVisible;
                m_CursorWhenDebugMenuOpened = CursorUtility.CursorIsShowing();
                SetMinimalVisible(true);
                m_DebugMenus.gameObject.SetActive(true);
                m_DebugMenuInput.interactable = true;
                m_MinimalGroup.interactable = true;
                if (!m_MenuUIInitialized) {
                    m_DebugMenus.GotoMenu(s_RootMenu);
                    m_MenuUIInitialized = true;
                }
                CursorUtility.ShowCursor();
                SetPaused(true);
            } else {
                if (!m_CursorWhenDebugMenuOpened) {
                    CursorUtility.HideCursor();
                }
                m_DebugMenus.gameObject.SetActive(false);
                m_DebugMenuInput.interactable = false;
                m_MinimalGroup.interactable = false;
                SetMinimalVisible(m_VisibilityWhenDebugMenuOpened);
                SetPaused(false);
            }
        }

        private void QueueSingleStep() {
            if (m_SingleStepState == StepState.Uninitialized) {
                m_SingleStepState = StepState.Queued;
                GameLoop.QueueEndOfFrame(AdvanceSingleStep);
            }
        }

        private void AdvanceSingleStep() {
            switch (m_SingleStepState) {
                case StepState.Executing: {
                    m_SingleStepState = StepState.Uninitialized;
                    SetPaused(true);
                    break;
                }
                case StepState.Queued: {
                    m_SingleStepState = StepState.Executing;
                    SetPaused(false);
                    GameLoop.QueueEndOfFrame(AdvanceSingleStep);
                    break;
                }
            }
        }

        #endregion // Menu

        #region Minimal Layer

        private void UpdateMinimalLayer() {
            if (Input.GetKeyDown(m_ToggleKey)) {
                SetMinimalVisible(!m_MinimalVisible);
            }
        }

        private void SetMinimalVisible(bool visible) {
            if (m_MinimalVisible == visible) {
                return;
            }

            m_MinimalVisible = visible;
            m_MinimalGroup.alpha = visible ? 1 : 0;
            m_MinimalGroup.blocksRaycasts = visible;
            m_Canvas.enabled = visible;

            if (!visible) {
                SetMenuVisible(false);
            }
        }

        #endregion // Minimal Layer

#endif // DEVELOPMENT
        }

    /// <summary>
    /// Attribute marking a static method to be invoked to create a root debug menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public sealed class DebugMenuFactoryAttribute : PreserveAttribute { }

    /// <summary>
    /// Attribute marking a static method to be invoked to create a quick debug menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public sealed class QuickMenuFactoryAttribute : PreserveAttribute { }

    /// <summary>
    /// Attribute marking a static method to be invoked to create an engine debug menu.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public sealed class EngineMenuFactoryAttribute : PreserveAttribute { }
}