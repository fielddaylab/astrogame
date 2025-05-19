using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using FieldDay.Rendering;

namespace Astro {
    public class InstrumentButtonMakeToggleTrigger : BatchedComponent, IRegistrationCallbacks {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private LabButton[] m_Buttons;
        [SerializeField] private bool m_InitVal;
        [SerializeField] private bool m_SwapTo = true;
        [SerializeField] private bool m_SetCollidersEnabled = true;

        public void OnDeregister() {
            m_Target.OnUnlock.Deregister(SwapButtons);
        }

        public void OnRegister() {
            m_Target.OnUnlock.Register(SwapButtons);
            InitButtons();
        }

        private void InitButtons() {
            foreach (var btn in m_Buttons) {
                btn.IsToggle = m_InitVal;
                if (m_SetCollidersEnabled) {
                    btn.GetComponent<Collider>().enabled = m_InitVal;
                }
            }
        }

        private void SwapButtons() {
            foreach (var btn in m_Buttons) {
                btn.IsToggle = m_SwapTo;
                if (m_SetCollidersEnabled) {
                    btn.GetComponent<Collider>().enabled = m_SwapTo;
                }
            }
        }
    }
}