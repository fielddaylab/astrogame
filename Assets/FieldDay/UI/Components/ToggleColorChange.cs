using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI {
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("Field Day/Canvas/Components/Toggle Color Change")]
    public class ToggleColorChange : MonoBehaviour, IOnGuiUpdate {
        private enum State {
            Disabled,
            Off,
            On
        }
        
        [Header("Colors")]
        [SerializeField] private ColorPalette2F m_OnPalette;
        [SerializeField] private ColorPalette2F m_OffPalette;
        [SerializeField] private bool m_UseDisabledPalette;
        [SerializeField, ShowIfField("m_UseDisabledPalette")] private ColorPalette2F m_DisabledPalette;
        
        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)] private ColorPaletteTargetSet2 m_Targets;
        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)] private ActiveGroup m_OnGroup;
        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)] private ActiveGroup m_OffGroup;

        [NonSerialized] private Toggle m_Toggle;
        [NonSerialized] private State m_LastState;

        private void Awake() {
            m_Toggle = GetComponent<Toggle>();
            ApplyState(GetState(m_Toggle, m_UseDisabledPalette));
        }

        private void OnEnable() {
            Game.Gui.RegisterUpdate(this);
        }

        private void OnDisable() {
            Game.Gui?.DeregisterUpdate(this);
        }

        void IOnGuiUpdate.OnGuiUpdate() {
            State nextState = GetState(m_Toggle, m_UseDisabledPalette);
            if (m_LastState != nextState) {
                ApplyState(nextState);
            }
        }

        private void ApplyState(State state) {
            ColorPalette2 palette = default;
            switch (state) {
                case State.Disabled:
                    palette = m_DisabledPalette;
                    break;
                case State.Off:
                    palette = m_OffPalette;
                    break;
                case State.On:
                    palette = m_OnPalette;
                    break;
            }

            if (!m_Targets.IsEmpty) {
                ColorPalette.Apply(palette, m_Targets);
            }
            m_LastState = state;
            m_OffGroup.SetActive(state != State.On);
            m_OnGroup.SetActive(state == State.On);
        }

        static private State GetState(Toggle toggle, bool useDisabled) {
            if (useDisabled && !toggle.interactable) {
                return State.Disabled;
            }
            return toggle.isOn ? State.On : State.Off;
        }
    }
}