using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    /// <summary>
    /// Marks Data Slot(s) as active on Instrument Unlock
    /// </summary>
    public class InstrumentDataSlotSetActiveTrigger : BatchedComponent, IRegistrationCallbacks
    {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private DataSlot[] m_Slots;
        [SerializeField] private bool m_InitVal;
        [SerializeField] private bool m_SetTo;

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
            foreach (var slot in m_Slots)
            {
                slot.IsActive = m_InitVal;
            }
        }

        private void SetVals()
        {
            foreach (var slot in m_Slots)
            {
                slot.IsActive = m_SetTo;
            }
        }
    }
}