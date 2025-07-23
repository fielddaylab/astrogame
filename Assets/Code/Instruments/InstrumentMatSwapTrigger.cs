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
        /// <summary>
        /// Assumed to be the deactivated material initally if this instrument has an inactive state
        /// </summary>
        [SerializeField] private Material m_InitMat;
        [SerializeField] private Material m_UnlockMat;

        public void OnDeregister() {
            m_Target.OnUnlock.Deregister(SwapMats);
        }

        public void OnRegister() {
            m_Target.OnUnlock.Register(SwapMats);
        }

        private void Awake() {
            SwapMats();
        }

        private void SwapToInitMats() {
            foreach (var renderer in m_Renderers) {
                renderer.SetSharedMaterialAtIndex(m_MatIndex, m_InitMat);
            }
        }

        public void SwapMats() {
            if (m_Target.Unlocked) {
                SwapToUnlockMats();
            } else {
                SwapToInitMats();
            }
        }

        private void SwapToUnlockMats() {
            foreach (var renderer in m_Renderers) {
                renderer.SetSharedMaterialAtIndex(m_MatIndex, m_UnlockMat);
            }
        }

        private IEnumerator SwapMatsRoutine() {
            float lerp = 0;
            float swapInflectionPoint = 0;
            while (lerp < swapInflectionPoint) {
                foreach (var renderer in m_Renderers) {
                    var mats = renderer.materials;
                    mats[m_MatIndex].Lerp(m_InitMat, m_UnlockMat, lerp);
                    renderer.materials = mats;
                }

                lerp += SwapSpeed * Time.deltaTime;

                yield return null;
            }

            foreach (var renderer in m_Renderers) {
                var mats = renderer.materials;
                mats[m_MatIndex] = m_UnlockMat;
                renderer.materials = mats;
            }

            while (lerp < 1) {
                foreach (var renderer in m_Renderers) {
                    var mats = renderer.materials;
                    mats[m_MatIndex].Lerp(m_InitMat, m_UnlockMat, lerp);
                    renderer.materials = mats;
                }

                lerp += SwapSpeed * Time.deltaTime;

                yield return null;
            }

            foreach (var renderer in m_Renderers) {
                var mats = renderer.materials;
                mats[m_MatIndex].Lerp(m_InitMat, m_UnlockMat, 1);
                renderer.materials = mats;
            }
        }
    }
}