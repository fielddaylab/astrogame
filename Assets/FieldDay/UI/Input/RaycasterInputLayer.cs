using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    [DisallowMultipleComponent]
    public sealed class RaycasterInputLayer : MonoBehaviour {
        public enum Mode {
            SetEnabled,
            SetMask
        }
        
        [SerializeField, Required] private BaseRaycaster m_Raycaster;
        [SerializeField] private Mode m_Mode;
        [SerializeField, ShowIfField("m_Mode")] private LayerMask m_Mask;

        [NonSerialized] private UpdateMaskDelegate m_MaskDelegate;

        private void Awake() {
            CacheMaskDelegate();
        }

        private void CacheMaskDelegate() {
            if (m_Raycaster == null) {
                m_MaskDelegate = UpdateNoOp;
            } else if (m_Raycaster is PhysicsRaycaster) {
                m_MaskDelegate = UpdatePhysicsRaycaster;
            } else if (m_Raycaster is Physics2DRaycaster) {
                m_MaskDelegate = UpdatePhysics2DRaycaster;
            } else if (m_Raycaster is FilteredGraphicRaycaster) {
                m_MaskDelegate = UpdateFilteredGraphicRaycaster;
            } else {
                m_MaskDelegate = UpdateNoOp;
            }
        }

        private void UpdateState(bool state) {
            switch (m_Mode) {
                case Mode.SetEnabled: {
                    m_Raycaster.enabled = state;
                    break;
                }

                case Mode.SetMask: {
                    LayerMask m = m_MaskDelegate(m_Raycaster, m_Mask, state);
                    m_Raycaster.enabled = m.value != 0;
                    break;
                }
            }
        }

        #region Update Functions

        private delegate LayerMask UpdateMaskDelegate(BaseRaycaster raycaster, LayerMask mask, bool state);

        static private readonly UpdateMaskDelegate UpdatePhysicsRaycaster = (r, m, s) => {
            PhysicsRaycaster p = (PhysicsRaycaster) r;
            return p.eventMask = Bits.Set(p.eventMask, m, s);
        };

        static private readonly UpdateMaskDelegate UpdatePhysics2DRaycaster = (r, m, s) => {
            Physics2DRaycaster p = (Physics2DRaycaster) r;
            return p.eventMask = Bits.Set(p.eventMask, m, s);
        };

        static private readonly UpdateMaskDelegate UpdateFilteredGraphicRaycaster = (r, m, s) => {
            FilteredGraphicRaycaster p = (FilteredGraphicRaycaster) r;
            return p.eventMask = Bits.Set(p.eventMask, m, s);
        };

        static private readonly UpdateMaskDelegate UpdateNoOp = (r, m, s) => { return s ? m : 0; };

        #endregion // Update Functions

#if UNITY_EDITOR

        private void Reset() {
            if (!m_Raycaster) {
                m_Raycaster = GetComponent<BaseRaycaster>();
            }

            if (!Frame.IsActive(this)) {
                return;
            }

            CacheMaskDelegate();
        }

        private void OnValidate() {
            if (!Frame.IsActive(this)) {
                return;
            }

            CacheMaskDelegate();
        }

#endif // UNITY_EDITOR
    }
}