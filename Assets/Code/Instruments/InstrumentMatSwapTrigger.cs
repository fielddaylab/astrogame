using FieldDay;
using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;
using FieldDay.Rendering;

namespace Astro {
    [DefaultExecutionOrder(100)]
    public class InstrumentMatSwapTrigger : BatchedComponent, IRegistrationCallbacks {
        [SerializeField] private LabInstrument m_Target;
        [SerializeField] private MeshRenderer[] m_Renderers;
        [SerializeField] private int m_MatIndex;
        [SerializeField] private float SwapSpeed = 1;
        [SerializeField] private Material m_InitMat;
        [SerializeField] private Material m_SwapTo;

        public void OnDeregister() {
            m_Target.OnUnlock.Deregister(SwapMats);
        }

        public void OnRegister() {
            m_Target.OnUnlock.Register(SwapMats);

            if (m_Target.Unlocked) {
                SwapMats();
            } else {
                InitMats();
            }
        }

        private void InitMats() {
            foreach (var renderer in m_Renderers) {
                renderer.SetSharedMaterialAtIndex(m_MatIndex, m_InitMat);
            }
        }

        private void SwapMats() {
            foreach (var renderer in m_Renderers) {
                renderer.SetSharedMaterialAtIndex(m_MatIndex, m_SwapTo);
            }
        }

        private IEnumerator SwapMatsRoutine() {
            float lerp = 0;
            float swapInflectionPoint = 0;
            while (lerp < swapInflectionPoint) {
                foreach (var renderer in m_Renderers) {
                    var mats = renderer.materials;
                    mats[m_MatIndex].Lerp(m_InitMat, m_SwapTo, lerp);
                    renderer.materials = mats;
                }

                lerp += SwapSpeed * Time.deltaTime;

                yield return null;
            }

            foreach (var renderer in m_Renderers) {
                var mats = renderer.materials;
                mats[m_MatIndex] = m_SwapTo;
                renderer.materials = mats;
            }

            while (lerp < 1) {
                foreach (var renderer in m_Renderers) {
                    var mats = renderer.materials;
                    mats[m_MatIndex].Lerp(m_InitMat, m_SwapTo, lerp);
                    renderer.materials = mats;
                }

                lerp += SwapSpeed * Time.deltaTime;

                yield return null;
            }

            foreach (var renderer in m_Renderers) {
                var mats = renderer.materials;
                mats[m_MatIndex].Lerp(m_InitMat, m_SwapTo, 1);
                renderer.materials = mats;
            }
        }
    }
}