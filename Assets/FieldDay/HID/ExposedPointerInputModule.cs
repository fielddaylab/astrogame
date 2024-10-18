using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.HID {
    public enum PointerInputMode : byte {
        Mouse,
        Touch
    }

    /// <summary>
    /// Pointer input module, with the current event data exposed.
    /// </summary>
    public class ExposedPointerInputModule : StandaloneInputModule {
        private TMP_InputField m_EditingText;
        private PointerEventData m_MousePointerData;
        private PointerEventData m_TouchPointerData;
        private PointerInputMode m_Mode;
        private bool m_Paused = true;

        /// <summary>
        /// Invoked when the input mode is changed.
        /// </summary>
        public event Action<PointerInputMode> OnModeChanged;

        /// <summary>
        /// Invoked when the current pointer raycast changes;
        /// </summary>
        public event Action<GameObject> OnPointerOverChanged;

        /// <summary>
        /// Invoked when a text input field focus is changed.
        /// </summary>
        public event Action<TMP_InputField> OnTextEditFocusChanged;

        /// <summary>
        /// The current input mode.
        /// </summary>
        public PointerInputMode Mode {
            get { return m_Mode; }
        }

        /// <summary>
        /// If text is currently being edited.
        /// </summary>
        public bool IsEditingText {
            get { return m_EditingText; }
        }

        /// <summary>
        /// The text currently being edited.
        /// </summary>
        public TMP_InputField TextEditTarget {
            get { return m_EditingText; }
        }

        /// <summary>
        /// Retrieves the current pointer event data.
        /// </summary>
        public PointerEventData GetPointerEventData() {
            switch (m_Mode) {
                case PointerInputMode.Mouse: {
                    if (m_MousePointerData == null) {
                        GetPointerData(kMouseLeftId, out m_MousePointerData, true);
                    }
                    return m_MousePointerData;
                }
                case PointerInputMode.Touch: {
                    if (m_TouchPointerData == null) {
                        GetPointerData(0, out m_TouchPointerData, true);
                    }
                    return m_TouchPointerData;
                }
                default: {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        #region Checks

        /// <summary>
        /// Returns if the current pointer is over a canvas.
        /// </summary>
        public bool IsPointerOverCanvas() {
            PointerEventData evtData = GetPointerEventData();
            
            if (evtData != null) {
                var baseRaycaster = evtData.pointerCurrentRaycast.module;
                return baseRaycaster && baseRaycaster is GraphicRaycaster;
            }

            return false;
        }

        /// <summary>
        /// Returns the object the pointer is currently over.
        /// </summary>
        public GameObject CurrentPointerOver() {
            PointerEventData evtData = GetPointerEventData();

            if (evtData != null) {
                return evtData.pointerCurrentRaycast.gameObject;
            }

            return null;
        }

        #endregion // Checks

        #region Overrides

        public override void Process() {
            if (m_Paused) {
                return;
            }

            GameObject prevOver = CurrentPointerOver();

            base.Process();

            if (m_Mode == PointerInputMode.Mouse) {
                if (UnityEngine.Input.touchCount > 0 || !UnityEngine.Input.mousePresent) {
                    m_Mode = PointerInputMode.Touch;
                    OnModeChanged?.Invoke(m_Mode);
                }
            } else {
                if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1) || UnityEngine.Input.GetMouseButtonDown(2)) {
                    m_Mode = PointerInputMode.Mouse;
                    OnModeChanged?.Invoke(m_Mode);
                }
            }

            TMP_InputField inputField = m_EditingText;
            if (!ReferenceEquals(inputField, null) && !inputField) {
                inputField = null;
            }

            GameObject focus = eventSystem.currentSelectedGameObject;
            if (focus != null) {
                if (focus.TryGetComponent(out TMP_InputField selectedInputField) && selectedInputField.isFocused) {
                    inputField = selectedInputField;
                }
            }

            GameObject over = CurrentPointerOver();
            if (over != prevOver) {
                OnPointerOverChanged?.Invoke(over);
            }

            if (m_EditingText != inputField) {
                m_EditingText = inputField;
                OnTextEditFocusChanged?.Invoke(inputField);
            }
        }

        protected override void Awake() {
            base.Awake();

            if (!UnityEngine.Input.mousePresent || UnityEngine.Input.touchSupported) {
                m_Mode = PointerInputMode.Touch;
            } else {
                m_Mode = PointerInputMode.Mouse;
            }
        }

        public override void ActivateModule() {
            base.ActivateModule();

            m_Paused = false;
        }

        public override void DeactivateModule() {
            base.DeactivateModule();

            if (m_EditingText != null) {
                m_EditingText = null;
                OnTextEditFocusChanged?.Invoke(null);
            }

            m_TouchPointerData = null;
            m_MousePointerData = null;
            m_Paused = true;
        }

        #endregion // Overrides
    }
}