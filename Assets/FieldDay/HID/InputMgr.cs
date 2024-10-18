using System;
using System.Diagnostics;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using NativeUtils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FieldDay.HID {
    public sealed class InputMgr {
        public const float DefaultDoubleClickBuffer = 0.8f;

        #region Types

        private struct InputTimestamp {
            public ushort FrameIndex;
            public long Ticks;

            static internal InputTimestamp Now() {
                return new InputTimestamp() {
                    FrameIndex = Frame.Index,
                    Ticks = Frame.Timestamp()
                };
            }
        }

        #endregion // Types

        #region State

        private EventSystem m_EventSystem;
        private BaseInputModule m_DefaultInputModule;
        private ExposedPointerInputModule m_ExposedInputModule;
        private uint m_ForceClickRecurseCounter;
        private RingBuffer<InputTimestamp> m_ClickTimestampBuffer = new RingBuffer<InputTimestamp>(2, RingBufferMode.Overwrite);
        private uint m_EventPauseCounter;
        private bool m_InputConsumed;

        #endregion // State

        internal InputMgr() { }

        #region Clicks

        /// <summary>
        /// Attempts to execute click handlers on the given object.
        /// </summary>
        public bool ExecuteClick(GameObject root) {
            Assert.NotNull(root);

            RectTransform rect = root.transform as RectTransform;
            if (rect && !rect.IsPointerInteractable()) {
                return false;
            }

            return ForceClick(root);
        }

        /// <summary>
        /// Forces the execution of click handlers on the given object.
        /// </summary>
        public bool ForceClick(GameObject root) {
            Assert.NotNull(m_ExposedInputModule);

            m_ForceClickRecurseCounter++;
            bool success = ExecuteEvents.Execute(root, m_ExposedInputModule.GetPointerEventData(), ExecuteEvents.pointerClickHandler);
            m_ForceClickRecurseCounter--;
            return success;
        }

        /// <summary>
        /// Returns if a forced input is being executed.
        /// </summary>
        public bool IsForcingClick() {
            return m_ForceClickRecurseCounter > 0;
        }

        /// <summary>
        /// Returns if a double click has occured recently.
        /// </summary>
        public bool HasDoubleClicked(float buffer = DefaultDoubleClickBuffer) {
            if (m_ClickTimestampBuffer.Count < 2 || m_InputConsumed) {
                return false;
            }

            long timeSince = m_ClickTimestampBuffer[0].Ticks - m_ClickTimestampBuffer[1].Ticks;
            long bufferTicks = (long) (buffer * Stopwatch.Frequency);
            return m_ClickTimestampBuffer[0].FrameIndex == Frame.Index && timeSince <= bufferTicks;
        }

        /// <summary>
        /// Returns if a double click has occured recently,
        /// and clears the double click buffer.
        /// </summary>
        public bool ConsumeDoubleClick(float buffer = DefaultDoubleClickBuffer) {
            if (HasDoubleClicked(buffer)) {
                m_ClickTimestampBuffer.Clear();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns if a mouse button is down this frame.
        /// </summary>
        public bool IsMouseDown(MouseButton mouseButton) {
            return !m_InputConsumed && Input.GetMouseButton((int) mouseButton);
        }

        /// <summary>
        /// Returns if a mouse button is down this frame.
        /// </summary>
        public bool IsMouseDown(int mouseButton) {
            return !m_InputConsumed && Input.GetMouseButton(mouseButton);
        }

        /// <summary>
        /// Returns if a mouse button was pressed this frame.
        /// </summary>
        public bool IsMousePressed(MouseButton mouseButton) {
            return !m_InputConsumed && Input.GetMouseButtonDown((int) mouseButton);
        }

        /// <summary>
        /// Returns if a mouse button was pressed this frame.
        /// </summary>
        public bool IsMousePressed(int mouseButton) {
            return !m_InputConsumed && Input.GetMouseButtonDown(mouseButton);
        }

        #endregion // Clicks

        #region Keys

        /// <summary>
        /// Returns if a keyboard key is down this frame.
        /// </summary>
        public bool IsKeyDown(KeyCode keyCode) {
            return !m_InputConsumed && keyCode > 0 && Input.GetKey(keyCode);
        }

        /// <summary>
        /// Returns if a keyboard key was pressed this frame.
        /// </summary>
        public bool IsKeyPressed(KeyCode keyCode) {
            return !m_InputConsumed && keyCode > 0 && Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Returns if a combination of a keyboard modifier and keyboard key
        /// were pressed this frame.
        /// </summary>
        public bool IsKeyComboPressed(ModifierKeyCode modifier, KeyCode keyCode) {
            return !m_InputConsumed && keyCode > 0 && Input.GetKeyDown(keyCode) && (modifier == 0 || Input.GetKey((KeyCode) modifier));
        }

        #endregion // Keys

        #region Raycasts

        /// <summary>
        /// Returns the object the pointer is currently over.
        /// </summary>
        public GameObject CurrentPointerOver() {
            return m_ExposedInputModule ? m_ExposedInputModule.CurrentPointerOver() : m_EventSystem.currentSelectedGameObject;
        }

        /// <summary>
        /// Returns if the pointer is over a canvas.
        /// </summary>
        public bool IsPointerOverCanvas() {
            if (m_ExposedInputModule != null) {
                return m_ExposedInputModule.IsPointerOverCanvas();
            } else {
                return m_EventSystem.IsPointerOverGameObject();
            }
        }

        /// <summary>
        /// Returns if the pointer is over a given hierarchy.
        /// </summary>
        public bool IsPointerOverHierarchy(Transform root) {
            if (root == null) {
                return false;
            }
            GameObject over = CurrentPointerOver();
            return over != null && over.transform.IsChildOf(root);
        }

        /// <summary>
        /// Returns if the pointer is over a given layer.
        /// </summary>
        public bool IsPointerOverLayer(LayerMask layerMask) {
            GameObject over = CurrentPointerOver();
            return over != null && (layerMask & (1 << over.layer)) != 0;
        }

        #endregion // Raycasts

        #region Events

        internal void Initialize() {
            m_EventSystem = EventSystem.current;
            m_DefaultInputModule = m_EventSystem?.currentInputModule;
            m_ExposedInputModule = m_DefaultInputModule as ExposedPointerInputModule;

            if (!m_ExposedInputModule) {
                Log.Warn("[InputMgr] Could not find ExposedInputInputModule");
            }

            GameLoop.OnGuiEvent.Register(OnGui);

            NativeInput.Initialize();
            NativeInput.SetEventSystem(m_EventSystem);
        }

        internal void BeginFrame() {
            if (Input.GetMouseButtonDown(0)) {
                m_ClickTimestampBuffer.PushFront(InputTimestamp.Now());
            }

            m_InputConsumed = false;
        }

        internal void OnGui(Event evt) {
            EventType type = evt.type;
            if ((type != EventType.KeyDown && type != EventType.KeyUp) || evt.keyCode == KeyCode.None) {
                return;
            }

            // TODO: block input if all is paused
            if (m_ExposedInputModule != null && m_ExposedInputModule.IsEditingText) {
                return;
            }

            if (type == EventType.KeyDown) {
                KeyboardUtility.OnKeyPressed.Invoke(evt.keyCode);
            } else {
                KeyboardUtility.OnKeyReleased.Invoke(evt.keyCode);
            }
        }

        internal void Shutdown() {
            NativeInput.SetEventSystem(null);
            NativeInput.Shutdown();

            GameLoop.OnGuiEvent.Deregister(OnGui);

            m_EventSystem = null;
            m_ExposedInputModule = null;
        }

        #endregion // Events

        #region Pausing

        /// <summary>
        /// Returns if all raycasting is paused.
        /// </summary>
        public bool AreRaycastsPaused() {
            return m_EventPauseCounter > 0;
        }

        /// <summary>
        /// Pauses all raycasting.
        /// </summary>
        public void PauseRaycasts() {
            if (m_EventPauseCounter++ == 0) {
                m_EventSystem.SetSelectedGameObject(null);
                m_DefaultInputModule.DeactivateModule();
                NativeInput.SetEventSystemEnabled(false);
            }
        }

        /// <summary>
        /// Resumes all raycasting.
        /// </summary>
        public void ResumeRaycasts() {
            if (m_EventPauseCounter > 0 && m_EventPauseCounter-- == 1) {
                m_DefaultInputModule.ActivateModule();
                NativeInput.SetEventSystemEnabled(true);
            }
        }

        #endregion // Pausing

        #region Consume

        /// <summary>
        /// Consumes all input for this frame.
        /// </summary>
        public void ConsumeAllInputForFrame() {
            m_InputConsumed = true;
            DebugInput.ConsumeAllForFrame();
        }

        #endregion // Consume
    }

    public enum ModifierKeyCode {
        LeftControl = KeyCode.LeftControl,
        LCtrl = KeyCode.LeftControl,
        RightControl = KeyCode.RightControl,
        RCtrl = KeyCode.RightControl,
        
        LeftShift = KeyCode.LeftShift,
        LShfit = KeyCode.LeftShift,
        RightShift = KeyCode.RightShift,
        RShift = KeyCode.RightShift,
        
        LeftAlt = KeyCode.LeftAlt,
        LAlt = KeyCode.LeftAlt,
        RightAlt = KeyCode.RightAlt,
        RAlt = KeyCode.RightAlt,
        
        LeftMeta = KeyCode.LeftMeta,
        LMeta = KeyCode.LeftMeta,
        RightMeta = KeyCode.RightMeta,
        RMeta = KeyCode.RightMeta
    }

    [Flags]
    public enum InputModifierKeys : uint {
        Ctrl = 0x01,
        Shift = 0x02,
        Alt = 0x04,
        Platform = 0x08,

        CtrlAlt = Ctrl | Alt,
        CtrlShift = Ctrl | Shift,
        AltShift = Alt | Shift,
        CtrlAltShift = Ctrl | Alt | Shift,

        L1 = 0x10,
        R1 = 0x20,
        L2 = 0x40,
        R2 = 0x80,

        BothGripButtons = L1 | R1,
        BothTriggerButtons = L2 | R2,

        BothShoulderButtons = L1 | R1,
        BothShoulderTriggers = L2 | R2,
    }

    public enum MouseButton {
        Left,
        Right,
        Middle
    }
}