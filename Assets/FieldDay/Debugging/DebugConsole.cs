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
using FieldDay.Audio;
using FieldDay.Data;
using FieldDay.HID;
using FieldDay.HID.XR;
using FieldDay.Perf;
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
        [SerializeField] private CanvasGroup m_KeyboardReferenceGroup = null;

        [Header("Debug Camera")]
        [SerializeField] private ConsoleCamera m_DebugCamera = null;
        [SerializeField] private GameObject m_DebugCameraReference = null;

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

        [NonSerialized] private bool m_CameraLock;
        [NonSerialized] private Vector2 m_CameraCursorPivot;

        static private DMInfo s_RootMenu;
        static private DMInfo s_QuickMenu;

        private void Awake() {
            GameLoop.OnDebugUpdate.Register(OnPreUpdate);
            GameLoop.QueuePreUpdate(LoadMenu);
            GameLoop.OnFrameAdvance.Register(AdvanceSingleStep);

            GameLoop.OnCrashReport.Register(() => {
                Destroy(gameObject);
            });
        }

        private void Start() {
            m_DebugMenus.gameObject.SetActive(false);
            m_Canvas.enabled = false;
            m_MinimalGroup.blocksRaycasts = false;

            m_KeyboardReferenceGroup.gameObject.SetActive(false);
            m_DebugCameraReference.SetActive(false);

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
            CheckCameraControls();
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
                DebugInput.ConsumeAllForFrame();
            }

            if (DebugInput.IsPressed(InputModifierKeys.CtrlShift, KeyCode.F8)
                || DebugInput.IsPressed(InputModifierKeys.CtrlShift, KeyCode.L)
                || DebugInput.IsPressed(InputModifierKeys.L1, XRHandIndex.Left, XRHandButtons.Menu)) {
                if (DebugDraw.IsRenderingEnabled()) {
                    DebugDraw.DisableRendering();
                } else {
                    DebugDraw.EnableRendering();
                }
                DebugInput.ConsumeAllForFrame();
            }

            if (m_MinimalVisible) {
                if (DebugInput.IsPressed(KeyCode.Backslash)) {
                    m_KeyboardReferenceGroup.gameObject.SetActive(!m_KeyboardReferenceGroup.gameObject.activeSelf);
                    DebugInput.ConsumeAllForFrame();
                }
            }
        }

        private void CheckCameraControls() {
            if (DebugFlags.IsAutomatedTestActive()) {
                ClearFreecam();
                return;
            }

            if (DebugInput.IsPressed(InputModifierKeys.Shift, KeyCode.C)) {
                Game.Input.ConsumeAllInputForFrame();

                if (m_DebugCamera.Camera()) {
                    ClearFreecam();
                } else {
                    Camera freeCam = Game.Rendering.PrimaryCamera;
                    if (!freeCam) {
                        freeCam = Camera.main;
                    }

                    if (!freeCam) {
                        Log.Error("[DebugConsole] No camera available for freecam");
                        DebugDraw.AddLogText("No camera available for freecam", Color.red, 1);
                    } else {
                        SetMinimalVisible(true);
                        m_DebugCameraReference.SetActive(true);
                        m_DebugCamera.SetCamera(freeCam);
                    }
                }
            }

            if (m_DebugCamera.Camera()) {
                bool hadInput = false;
                Vector3 move = default;

                if (DebugInput.IsPressed(KeyCode.F)) {
                    m_CameraLock = !m_CameraLock;
                    hadInput = true;
                }

                if (!m_CameraLock) {
                    if (DebugInput.IsDown(DebugInputButtons.DPadLeft)) {
                        move.x -= 1;
                        hadInput = true;
                    }
                    if (DebugInput.IsDown(DebugInputButtons.DPadRight)) {
                        move.x += 1;
                        hadInput = true;
                    }
                    if (DebugInput.IsDown(DebugInputButtons.DPadUp)) {
                        move.z += 1;
                        hadInput = true;
                    }
                    if (DebugInput.IsDown(DebugInputButtons.DPadDown)) {
                        move.z -= 1;
                        hadInput = true;
                    }
                    if (DebugInput.IsDown(KeyCode.Q)) {
                        move.y -= 1;
                        hadInput = true;
                    }
                    if (DebugInput.IsDown(KeyCode.E)) {
                        move.y += 1;
                        hadInput = true;
                    }

                    if (DebugInput.IsDown(KeyCode.LeftShift)) {
                        move *= 4;
                    }

                    m_DebugCamera.MoveRelative(move * Time.unscaledDeltaTime);
                }

                if (DebugInput.IsPressed(MouseButton.Right)) {
                    m_CameraCursorPivot = Input.mousePosition;
                    hadInput = true;

#if UNITY_EDITOR
                    UnityEditor.EditorGUIUtility.SetWantsMouseJumping(1);
#endif // UNITY_EDITOR
                } else if (DebugInput.IsDown(MouseButton.Right)) {
                    Vector2 newPos = Input.mousePosition;
                    Vector2 mouseShift = newPos - m_CameraCursorPivot;
                    m_CameraCursorPivot = newPos;

                    Vector3 eulerShift;
                    eulerShift.x = -mouseShift.y;
                    eulerShift.y = mouseShift.x;
                    eulerShift.z = 0;

                    m_DebugCamera.Rotate(eulerShift * Time.unscaledDeltaTime);
                    hadInput = true;
                } else {
#if UNITY_EDITOR
                    UnityEditor.EditorGUIUtility.SetWantsMouseJumping(0);
#endif // UNITY_EDITOR
                }

                if (hadInput) {
                    Game.Input.ConsumeAllInputForFrame();
                }
            }
        }

        #endregion // Keyboard Shortcuts

        #region Time Scale

        private void CheckTimeInput() {
            if (!DebugFlags.AllowTimeControl() || DebugFlags.IsAutomatedTestActive()) {
                return;
            }

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
                if (Input.GetKeyDown(KeyCode.Minus)) {
                    UpdateTimescale(m_TimeScale / 2);
                } else if (Input.GetKeyDown(KeyCode.Equals)) {
                    if (m_TimeScale * 2 < 100) {
                        UpdateTimescale(m_TimeScale * 2);
                    }
                } else if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    UpdateTimescale(1);
                } else if (Input.GetKeyDown(KeyCode.Alpha9)) {
                    QueueSingleStep();
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

            AudioPropertyBlock debugAudioProps = Game.Audio.GetDebugProperties(AudioBus.Master);
            debugAudioProps.Pitch = m_TimeScale;
            debugAudioProps.Volume = Math.Min(1, Mathf.Sqrt(1f / m_TimeScale));
            Game.Audio.SetDebugProperties(AudioBus.Master, debugAudioProps);
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

            AudioPropertyBlock debugAudioProps = Game.Audio.GetDebugProperties(AudioBus.Master);
            debugAudioProps.Pause = paused;
            Game.Audio.SetDebugProperties(AudioBus.Master, debugAudioProps);

            if (paused) {
                Time.timeScale = 0;
                m_TimeDisplay.UpdateState(true);
                OnTimeScaleUpdated.Invoke(0);
                EventSystem.current?.SetSelectedGameObject(null);
                Game.Input.SetDebugPauseOverride(true);
            } else {
                Time.timeScale = m_TimeScale;
                m_TimeDisplay.UpdateState(false);
                OnTimeScaleUpdated.Invoke(m_TimeScale);
                Game.Input.SetDebugPauseOverride(false);
            }
        }

        #endregion // Time Scale

        #region Menu

        private void UpdateMenu() {

            bool canHaveMenuOpen = !GameLoop.IsLoading && !DebugFlags.IsAutomatedTestActive();

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W) && canHaveMenuOpen) {
                SetMenuVisible(!m_MenuOpen);
                Game.Input.ConsumeAllInputForFrame();
            }

            if (m_DebugMenus.isActiveAndEnabled) {
                if (!canHaveMenuOpen) {
                    SetMenuVisible(false);
                } else {
                    m_DebugMenus.UpdateElements();
                    m_DebugMenus.SubmitCommand(GetMenuCommand());
                }
            }
        }

        static private DMMenuUI.NavigationCommand GetMenuCommand() {
            if (DebugInput.IsPressed(DebugInputButtons.Cancel)) {
                return DMMenuUI.NavigationCommand.Back;
            } else if (DebugInput.IsPressed(DebugInputButtons.DPadLeft)) {
                return DMMenuUI.NavigationCommand.PrevPage;
            } else if (DebugInput.IsPressed(DebugInputButtons.DPadRight)) {
                return DMMenuUI.NavigationCommand.NextPage;
            } else if (DebugInput.IsPressed(DebugInputButtons.DPadUp)) {
                return DMMenuUI.NavigationCommand.MoveArrowUp;
            } else if (DebugInput.IsPressed(DebugInputButtons.DPadDown)) {
                return DMMenuUI.NavigationCommand.MoveArrowDown;
            } else if (DebugInput.IsPressed(DebugInputButtons.Select)) {
                return DMMenuUI.NavigationCommand.SelectArrow;
            } else {
                return DMMenuUI.NavigationCommand.None;
            }
        }

        static private void LoadMenu() {
            LoadRootMenu();
            LoadQuickMenu();
        }

        static private void LoadRootMenu() {
            s_RootMenu = new DMInfo("Debug", 16);

            // load menus from user assemblies
            foreach (var pair in ReflectionBootData.DebugMenus()) {
                MethodInfo method = (MethodInfo) pair.Info;

                if (method.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                if (method.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) method.Invoke(null, Array.Empty<object>());

                if (menu != null) {
                    DMInfo.MergeSubmenu(s_RootMenu, menu, true);
                }
            }

            // load engine menus from user assemblies
            DMInfo engineMenu = new DMInfo("Engine", 16);
            foreach (var pair in ReflectionBootData.EngineMenus()) {
                MethodInfo method = (MethodInfo) pair.Info;

                if (method.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                if (method.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.Name, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) method.Invoke(null, Array.Empty<object>());

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
            foreach (var pair in ReflectionBootData.QuickMenus()) {
                MethodInfo method = (MethodInfo) pair.Info;

                if (method.ReturnType != typeof(DMInfo)) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' does not return DMInfo", pair.Info.DeclaringType.FullName, pair.Info.Name);
                    continue;
                }

                if (method.GetParameters().Length != 0) {
                    Log.Error("[DebugConsole] Method '{0}::{1}' has parameters", pair.Info.DeclaringType.FullName, pair.Info.Name);
                    continue;
                }

                DMInfo menu = (DMInfo) method.Invoke(null, Array.Empty<object>());

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
            }
        }

        private void AdvanceSingleStep() {
            switch (m_SingleStepState) {
                case StepState.Executing: {
                    m_SingleStepState = StepState.Uninitialized;
                    SetPaused(true);
                    SetMenuVisible(true);
                    SetMinimalVisible(true);
                    break;
                }
                case StepState.Queued: {
                    m_SingleStepState = StepState.Executing;
                    SetPaused(false);
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

            if (visible) {
                FramerateDisplay.Hide();
            } else {
                FramerateDisplay.Show();
            }

            if (!visible) {
                SetMenuVisible(false);
                ClearFreecam();
            }
        }

        #endregion // Minimal Layer

        #region Freecam

        private void ClearFreecam() {
            if (m_DebugCamera.SetCamera(null)) {
                m_DebugCameraReference.SetActive(false);
                m_CameraLock = false;

#if UNITY_EDITOR
                UnityEditor.EditorGUIUtility.SetWantsMouseJumping(0);
#endif // UNITY_EDITOR
            }
        }

        #endregion // Freecam

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