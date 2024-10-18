#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;

using StateIndex = BeauUtil.TypeIndex<FieldDay.SharedState.ISharedState>;

namespace FieldDay.SharedState {
    /// <summary>
    /// Manager for shared singleton state objects.
    /// This maintains access to singleton state objects by type.
    /// </summary>
    public sealed class SharedStateMgr {
        static private readonly StaticInjector<SharedStateReferenceAttribute, ISharedState> s_StaticInjector = new StaticInjector<SharedStateReferenceAttribute, ISharedState>();

        private ISharedState[] m_StateMap = new ISharedState[StateIndex.Capacity];
        private readonly HashSet<ISharedState> m_StateSet = new HashSet<ISharedState>(32);

        internal SharedStateMgr() { }

        #region Add/Remove

        /// <summary>
        /// Registers the given ISharedState instance.
        /// </summary>
        public void Register(ISharedState state) {
            Assert.NotNull(state);
            
            Type stateType = state.GetType();
            int index = StateIndex.Get(stateType);

            Assert.True(m_StateMap[index] == null, "[SharedStateMgr] Shared state of type '{0}' already registered", stateType);
            m_StateMap[index] = state;
            m_StateSet.Add(state);

            s_StaticInjector.Inject(state);
            RegistrationCallbacks.InvokeRegister(state);
            Log.Msg("[SharedStateMgr] State '{0}' registered", stateType.FullName);
        }

        /// <summary>
        /// Deregisters the given ISharedState instance.
        /// </summary>
        public void Deregister(ISharedState state) {
            Assert.NotNull(state);
            
            Type stateType = state.GetType();
            int index = StateIndex.Get(stateType);

            if (m_StateMap[index] == state) {
                m_StateMap[index] = null;
                m_StateSet.Remove(state);

                s_StaticInjector.Remove(state);
                RegistrationCallbacks.InvokeDeregister(state);
                Log.Msg("[SharedStateMgr] State '{0}' deregistered", stateType.FullName);
            }
        }

        /// <summary>
        /// Clears all ISharedState instances.
        /// </summary>
        public void Clear() {
            foreach(var state in m_StateSet) {
                s_StaticInjector.Remove(state);
                RegistrationCallbacks.InvokeDeregister(state);
            }
            m_StateSet.Clear();
            Array.Clear(m_StateMap, 0, m_StateMap.Length);
        }

        #endregion // Add/Remove

        #region Lookup

        /// <summary>
        /// Returns the shared state object of the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISharedState Get(Type type) {
            int index = StateIndex.Get(type);
            ISharedState state = m_StateMap[index];
#if DEVELOPMENT
            if (state == null) {
                Assert.Fail("No shared state object found for type '{0}'", type.FullName);
            }
#endif // DEVELOPMENT
            return state;
        }

        /// <summary>
        /// Returns the shared state object for the given type.
        /// This will assert if none is found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get<T>() where T : class, ISharedState {
            int index = StateIndex.Get<T>();
            ISharedState state = m_StateMap[index];
#if DEVELOPMENT
            if (state == null) {
                Assert.Fail("No shared state object found for type '{0}'", typeof(T).FullName);
            }
#endif // DEVELOPMENT
            return (T) state;
        }

        /// <summary>
        /// Fast unchecked retrieve.
        /// </summary>
        internal T FastGet<T>() where T : class, ISharedState {
            return (T) m_StateMap[StateIndex.Get<T>()];
        }

        /// <summary>
        /// Attempts to return the shared state object for the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet(Type type, out ISharedState state) {
            int index = StateIndex.Get(type);
            state = index < m_StateMap.Length ? m_StateMap[index] : null;
            return state != null;
        }

        /// <summary>
        /// Attempts to return the shared state object for the given type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGet<T>(out T state) where T : class, ISharedState {
            int index = StateIndex.Get<T>();
            state = (T) (index < m_StateMap.Length ? m_StateMap[index] : null);
            return state != null;
        }

        /// <summary>
        /// Returns if the shared state of the given type exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has(Type type) {
            int index = StateIndex.Get(type);
            return index < m_StateMap.Length ? m_StateMap[index] != null : false;
        }

        /// <summary>
        /// Returns if the shared state of the given type exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Has<T>() where T : class, ISharedState {
            int index = StateIndex.Get<T>();
            return index < m_StateMap.Length ? m_StateMap[index] != null : false;
        }

        /// <summary>
        /// Looks up all shared states that pass the given predicate.
        /// </summary>
        public int LookupAll(Predicate<ISharedState> predicate, List<ISharedState> sharedStates) {
            int found = 0;
            foreach(var state in m_StateSet) {
                if (predicate(state)) {
                    sharedStates.Add(state);
                    found++;
                }
            }
            return found;
        }

        /// <summary>
        /// Looks up all shared states that implement the given interface or class.
        /// </summary>
        public int LookupAll<T>(List<T> sharedStates) where T : class {
            int found = 0;
            foreach (var state in m_StateSet) {
                T casted = state as T;
                if (casted != null) {
                    sharedStates.Add(casted);
                    found++;
                }
            }
            return found;
        }

        /// <summary>
        /// Looks up all shared states that pass the given predicate.
        /// </summary>
        public int LookupAll<U>(Predicate<ISharedState, U> predicate, U predicateArg, List<ISharedState> sharedStates) {
            int found = 0;
            foreach (var state in m_StateSet) {
                if (predicate(state, predicateArg)) {
                    sharedStates.Add(state);
                    found++;
                }
            }
            return found;
        }

        #endregion // Lookup

        #region Require

        /// <summary>
        /// Retrieves the currently registered instance of the given type.
        /// Will create a new instance if one is not registered.
        /// 
        /// NOTE: Calling this with UnityEngine.Object-derived objects may cause memory leaks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ISharedState Require(Type type) {
            int index = StateIndex.Get(type);
            ISharedState state;

            if ((state = m_StateMap[index]) == null) {
                state = CreateInstance(type);
                m_StateMap[index] = state;
                s_StaticInjector.Inject(state);
                RegistrationCallbacks.InvokeRegister(state);
                Log.Msg("[SharedStateMgr] State '{0}' created on Require", type.FullName);
            }
            return state;
        }

        /// <summary>
        /// Retrieves the currently registered instance of the given type.
        /// Will create a new instance if one is not registered.
        /// 
        /// NOTE: Calling this with UnityEngine.Object-derived objects may cause memory leaks.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Require<T>() where T : class, ISharedState {
            int index = StateIndex.Get<T>();
            ISharedState state;

            if ((state = m_StateMap[index]) == null) {
                Type type = typeof(T);
                state = CreateInstance(type);
                m_StateMap[index] = state;
                s_StaticInjector.Inject(state);
                RegistrationCallbacks.InvokeRegister(state);
                Log.Msg("[SharedStateMgr] State '{0}' created on Require", type.FullName);
            }
            return (T) state;
        }

        /// <summary>
        /// Creates a new instance of the given state type.
        /// </summary>
        static private ISharedState CreateInstance(Type stateType) {
            Assert.True(typeof(ISharedState).IsAssignableFrom(stateType), "Type '{0}' is not derived from ISharedState", stateType.FullName);
            if (typeof(UnityEngine.Object).IsAssignableFrom(stateType)) {
                Log.Error("[SharedStateMgr] Attempting to create instance of UnityEngine.Object derived class '{0}' - please don't use Require() for this type", stateType.FullName);
            }
            return (ISharedState) Activator.CreateInstance(stateType);
        }

        #endregion // Require

        #region Events

        internal void Shutdown() {
            m_StateSet.Clear();
            Array.Clear(m_StateMap, 0, m_StateMap.Length);
        }

        #endregion // Events
    }
}