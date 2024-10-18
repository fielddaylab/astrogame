using System;
using BeauUtil;
using FieldDay.SharedState;
using UnityEngine;

namespace FieldDay.Systems {
    /// <summary>
    /// System operating on a shared state instance.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TState> : MonoBehaviour, ISystem
        where TState : class, ISharedState {

        [NonSerialized] protected TState m_State;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_State = Game.SharedState.FastGet<TState>()) != null;
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
        }

        public virtual void Shutdown() {
            m_State = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on two shared state instance.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState {
        
        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_StateA = Game.SharedState.FastGet<TStateA>()) != null && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null;
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
        }

        public virtual void Shutdown() {
            m_StateA = null;
            m_StateB = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on three shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_StateA = Game.SharedState.FastGet<TStateA>()) != null && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null; 
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
        }

        public virtual void Shutdown() {
            m_StateA = null;
            m_StateB = null;
            m_StateC = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on four shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC, TStateD> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState
        where TStateD : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;
        [NonSerialized] protected TStateD m_StateD;

        #region Work

        public virtual bool HasWork() {
            return isActiveAndEnabled && (m_StateA = Game.SharedState.FastGet<TStateA>()) != null && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null;
        }

        public virtual void ProcessWork(float deltaTime) {
        }

        #endregion // Work

        #region Lifecycle

        public virtual void Initialize() {
        }

        public virtual void Shutdown() {
            m_StateA = null;
            m_StateB = null;
            m_StateC = null;
            m_StateD = null;
        }

        #endregion // Lifecycle
    }
}