using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InstrumentMatSwapTrigger : BatchedComponent, IRegistrationCallbacks
    {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private MeshRenderer[] m_Renderers;
        [SerializeField] private int m_MatIndex;
        [SerializeField] private Material m_InitMat;
        [SerializeField] private Material m_SwapTo;

        public void OnDeregister()
        {
            m_Target.OnUnlock.Deregister(SwapMats);
        }

        public void OnRegister()
        {
            m_Target.OnUnlock.Register(SwapMats);
            InitMats();
        }

        private void InitMats()
        {
            foreach (var renderer in m_Renderers)
            {
                var mats = renderer.sharedMaterials;
                mats[m_MatIndex] = m_InitMat;
                renderer.sharedMaterials = mats;
            }
        }

        private void SwapMats()
        {
            foreach (var renderer in m_Renderers)
            {
                var mats = renderer.sharedMaterials;
                mats[m_MatIndex] = m_SwapTo;
                renderer.sharedMaterials = mats;
            }
        }
    }
}