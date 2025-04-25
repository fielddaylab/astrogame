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
            return (m_State = Game.SharedState.FastGet<TState>()) != null;
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
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null;
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
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null; 
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
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null
                && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null;
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

    /// <summary>
    /// System operating on five shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC, TStateD, TStateE> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState
        where TStateD : class, ISharedState
        where TStateE : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;
        [NonSerialized] protected TStateD m_StateD;
        [NonSerialized] protected TStateE m_StateE;

        #region Work

        public virtual bool HasWork() {
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null
                && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null
                && (m_StateE = Game.SharedState.FastGet<TStateE>()) != null;
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
            m_StateE = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on six shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC, TStateD, TStateE, TStateF> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState
        where TStateD : class, ISharedState
        where TStateE : class, ISharedState
        where TStateF : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;
        [NonSerialized] protected TStateD m_StateD;
        [NonSerialized] protected TStateE m_StateE;
        [NonSerialized] protected TStateF m_StateF;

        #region Work

        public virtual bool HasWork() {
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null
                && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null
                && (m_StateE = Game.SharedState.FastGet<TStateE>()) != null
                && (m_StateF = Game.SharedState.FastGet<TStateF>()) != null;
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
            m_StateE = null;
            m_StateF = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on seven shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC, TStateD, TStateE, TStateF, TStateG> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState
        where TStateD : class, ISharedState
        where TStateE : class, ISharedState
        where TStateF : class, ISharedState
        where TStateG : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;
        [NonSerialized] protected TStateD m_StateD;
        [NonSerialized] protected TStateE m_StateE;
        [NonSerialized] protected TStateF m_StateF;
        [NonSerialized] protected TStateG m_StateG;

        #region Work

        public virtual bool HasWork() {
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null
                && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null
                && (m_StateE = Game.SharedState.FastGet<TStateE>()) != null
                && (m_StateF = Game.SharedState.FastGet<TStateF>()) != null
                && (m_StateG = Game.SharedState.FastGet<TStateG>()) != null;
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
            m_StateE = null;
            m_StateF = null;
            m_StateG = null;
        }

        #endregion // Lifecycle
    }

    /// <summary>
    /// System operating on eight shared state instances.
    /// </summary>
    [NonIndexed]
    public abstract class SharedStateSystemBehaviour<TStateA, TStateB, TStateC, TStateD, TStateE, TStateF, TStateG, TStateH> : MonoBehaviour, ISystem
        where TStateA : class, ISharedState
        where TStateB : class, ISharedState
        where TStateC : class, ISharedState
        where TStateD : class, ISharedState
        where TStateE : class, ISharedState
        where TStateF : class, ISharedState
        where TStateG : class, ISharedState
        where TStateH : class, ISharedState {

        [NonSerialized] protected TStateA m_StateA;
        [NonSerialized] protected TStateB m_StateB;
        [NonSerialized] protected TStateC m_StateC;
        [NonSerialized] protected TStateD m_StateD;
        [NonSerialized] protected TStateE m_StateE;
        [NonSerialized] protected TStateF m_StateF;
        [NonSerialized] protected TStateG m_StateG;
        [NonSerialized] protected TStateH m_StateH;

        #region Work

        public virtual bool HasWork() {
            return (m_StateA = Game.SharedState.FastGet<TStateA>()) != null
                && (m_StateB = Game.SharedState.FastGet<TStateB>()) != null
                && (m_StateC = Game.SharedState.FastGet<TStateC>()) != null
                && (m_StateD = Game.SharedState.FastGet<TStateD>()) != null
                && (m_StateE = Game.SharedState.FastGet<TStateE>()) != null
                && (m_StateF = Game.SharedState.FastGet<TStateF>()) != null
                && (m_StateG = Game.SharedState.FastGet<TStateG>()) != null
                && (m_StateH = Game.SharedState.FastGet<TStateH>()) != null;
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
            m_StateE = null;
            m_StateF = null;
            m_StateG = null;
            m_StateH = null;
        }

        #endregion // Lifecycle
    }
}