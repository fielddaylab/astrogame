using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scenes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InstrumentComponentSetActiveTrigger : BatchedComponent, IRegistrationCallbacks, IScenePreload
    {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private MonoBehaviour[] m_Components;
        [SerializeField] private bool m_InitVal;
        [SerializeField] private bool m_SetTo;

        public void OnDeregister()
        {
            m_Target.OnUnlock.Deregister(SetVals);
        }

        public void OnRegister()
        {
            m_Target.OnUnlock.Register(SetVals);
        }

        private void InitVals()
        {
            foreach (var comp in m_Components)
            {
                comp.enabled = m_InitVal;
            }
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            InitVals();
            return null;
        }

        private void SetVals()
        {
            foreach (var comp in m_Components)
            {
                comp.enabled = m_SetTo;
            }
        }
    }
}