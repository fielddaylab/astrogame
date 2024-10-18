using System;
using BeauUtil;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FieldDay.UI {
    /// <summary>
    /// Selectable state change handler.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public abstract class SelectableStateAnimator : MonoBehaviour, IOnGuiUpdate {
        [SerializeField, HideInInspector] private Selectable m_Selectable;
        [NonSerialized] private SelectionState m_LastState = SelectionState.Unset;

        [NonSerialized] private SelectionState? m_StateOverride;

        #region Unity Events

        protected virtual void Awake() {
            this.CacheComponent(ref m_Selectable);
        }

        protected virtual void OnEnable() {
            Game.Gui.RegisterUpdate(this);
        }

        protected virtual void OnDisable() {
            m_LastState = SelectionState.Unset;
            Game.Gui?.DeregisterUpdate(this);
        }

        #endregion // Unity Events

        /// <summary>
        /// Overrides the selection state.
        /// </summary>
        public void OverrideState(SelectionState? state) {
            m_StateOverride = state;
        }

        /// <summary>
        /// Clears the selection state override.
        /// </summary>
        public void ClearOverride() {
            m_StateOverride = null;
        }

        #region Handlers
        public abstract void HandleStateChanged(SelectionState state);

        public virtual void OnGuiUpdate() {
            SelectionState state = m_StateOverride.GetValueOrDefault(m_Selectable.GetSelectionState());
            if (Ref.ReplaceEnum(ref m_LastState, state)) {
                HandleStateChanged(state);
            }
        }

        #endregion // Handlers

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