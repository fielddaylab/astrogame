using System;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    /// <summary>
    /// Pointer click handler that only emits if interactive.
    /// </summary>
    public abstract class InteractableClickHandler : MonoBehaviour, IPointerClickHandler, IPointerUpHandler, IBaked {
        [SerializeField, HideInInspector] private Selectable m_Selectable;
        [NonSerialized] private bool m_WasSelectable;

        #region Unity Events

        protected virtual void Awake() {
            this.CacheComponent(ref m_Selectable);
        }

        protected virtual void OnEnable() {
        }

        protected virtual void OnDisable() {
            m_WasSelectable = false;
        }

        #endregion // Unity Events

        #region Handlers

        public abstract void HandleClick();

        #endregion // Handlers

        #region IPointer Events

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
            m_WasSelectable = !m_Selectable || m_Selectable.IsInteractable();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
            if (eventData.button != 0) {
                return;
            }

            if (Game.Input.IsForcingClick() || m_WasSelectable) {
                HandleClick();
            }

            m_WasSelectable = false;
        }

        #endregion // IPointer Events

        #region IBaked

#if UNITY_EDITOR

        protected virtual void Reset() {
            m_Selectable = GetComponent<Selectable>();
        }

        protected virtual void OnValidate() {
            this.CacheComponent(ref m_Selectable);
        }

        public int Order { get { return 100; } }

        public virtual bool Bake(BakeFlags flags, BakeContext context) {
            m_Selectable = GetComponent<Selectable>();
            return true;
        }

#endif // UNITY_EDITOR

        #endregion // IBaked
    }
}