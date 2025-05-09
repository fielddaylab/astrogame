using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    /// <summary>
    /// Marks renderers as active on Instrument Unlock
    /// </summary>
    public class InstrumentRendererSetActiveTrigger : BatchedComponent, IRegistrationCallbacks
    {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private Renderer[] m_Renderers;
        [SerializeField] private bool m_InitVal;
        [SerializeField] private bool m_SetTo = true;

        public void OnDeregister()
        {
            m_Target.OnUnlock.Deregister(SetVals);
        }

        public void OnRegister()
        {
            m_Target.OnUnlock.Register(SetVals);
            InitVals();
        }

        private void InitVals()
        {
            foreach (var slot in m_Renderers)
            {
                slot.enabled = m_InitVal;
            }
        }

        private void SetVals()
        {
            foreach (var slot in m_Renderers)
            {
                slot.enabled = m_SetTo;
            }
        }
    }
}