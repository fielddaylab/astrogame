using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InstrumentColorPanelMatSwapTrigger : BatchedComponent, IRegistrationCallbacks
    {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private ColorPanel[] m_Panels;
        [SerializeField] private int m_MatIndex;
        [SerializeField] private Material m_InitMat;

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
            foreach (var panel in m_Panels)
            {
                var mats = panel.IndicatorMesh.sharedMaterials;
                mats[m_MatIndex] = m_InitMat;
                panel.IndicatorMesh.sharedMaterials = mats;

                mats = panel.PanelMesh.sharedMaterials;
                mats[m_MatIndex] = m_InitMat;
                panel.PanelMesh.sharedMaterials = mats;
            }
        }

        private void SwapMats()
        {
            foreach (var panel in m_Panels)
            {
                ColorDataUtility.SetIndicatorMaterials(panel.IndicatorMesh, false);
                ColorDataUtility.SetPanelMaterials(panel.PanelMesh, panel.ColorId, false);
            }
        }
    }
}