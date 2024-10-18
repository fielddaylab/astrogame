using System;
using System.Collections;
using System.Reflection;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Processes {
    public delegate void ProcessCallback(Process process);
    public delegate void ProcessStateTransitionCallback(Process process, ProcessStateDefinition nextState);
    public delegate IEnumerator ProcessCoroutine(Process process);
    public delegate void ProcessUpdateCallback(Process process, float deltaTime);
    public delegate void ProcessSignalCallback(Process process, StringHash32 signalId, object signalArgs);

    /// <summary>
    /// Describes a process state, including the identifier, custom flags,
    /// and the methods called for specific process events and updates.
    /// </summary>
    public sealed class ProcessStateDefinition {
        public readonly StringHash32 Id;
        public readonly BitSet64 CustomFlags;
        public readonly ProcessStateVTable Table;
        public readonly object Target;

        public ProcessStateDefinition(StringHash32 id, ProcessStateVTable methods, BitSet64 customFlags = default, object target = null) {
            Id = id;
            CustomFlags = customFlags;

            methods.GeneratePhaseMask();
            Table = methods;
            Target = target;
        }

        public override string ToString() {
            return Id.ToDebugString();
        }

        /// <summary>
        /// Creates a process state definition from the given type's static methods.
        /// </summary>
        static public ProcessStateDefinition FromType(string name, Type type, BitSet64 customFlags = default) {
            return new ProcessStateDefinition(name, ProcessStateVTable.FromType(type), customFlags, type);
        }

        /// <summary>
        /// Creates a process state definition from the given type's static methods.
        /// </summary>
        static public ProcessStateDefinition FromTypePrefixed(string name, Type type, BitSet64 customFlags = default) {
            return new ProcessStateDefinition(name, ProcessStateVTable.FromTypePrefixed(name, type), customFlags, type);
        }

        /// <summary>
        /// Creates a process state definition from the given object's static methods.
        /// </summary>
        static public ProcessStateDefinition FromObject(string name, object target, BitSet64 customFlags = default) {
            return new ProcessStateDefinition(name, ProcessStateVTable.FromObject(target), customFlags, target);
        }

        /// <summary>
        /// Creates a process state definition from the given object's instance methods.
        /// </summary>
        static public ProcessStateDefinition FromObjectPrefixed(string name, object target, BitSet64 customFlags = default) {
            return new ProcessStateDefinition(name, ProcessStateVTable.FromObjectPrefixed(name, target), customFlags, target);
        }

        /// <summary>
        /// Creates a process state definition from the given callbacks object.
        /// </summary>
        static public ProcessStateDefinition FromCallbacks(StringHash32 id, IProcessStateCallbacks config, BitSet64 customFlags = default) {
            return new ProcessStateDefinition(id, ProcessStateVTable.FromCallbacks(config), customFlags, config);
        }
    }

    /// <summary>
    /// State method table.
    /// </summary>
    public struct ProcessStateVTable {
        #region Updates

        internal GameLoopPhaseMask UpdatePhaseMask;

        /// <summary>
        /// Invoked in the Pre-Update phase.
        /// </summary>
        public ProcessUpdateCallback OnPreUpdate;

        /// <summary>
        /// Invoked in the Fixed Update phase.
        /// </summary>
        public ProcessUpdateCallback OnFixedUpdate;

        /// <summary>
        /// Invoked after the Fixed Update phase.
        /// </summary>
        public ProcessUpdateCallback OnLateFixedUpdate;

        /// <summary>
        /// Invoked in the Update phase.
        /// </summary>
        public ProcessUpdateCallback OnUpdate;

        /// <summary>
        /// Invoked in the Unscaled Update phase.
        /// </summary>
        public ProcessUpdateCallback OnUnscaledUpdate;

        /// <summary>
        /// Invoked in the Late Update phase.
        /// </summary>
        public ProcessUpdateCallback OnLateUpdate;

        /// <summary>
        /// Invoked in the Unscaled Late Update phase.
        /// </summary>
        public ProcessUpdateCallback OnUnscaledLateUpdate;

        #endregion // Updates

        #region Events

        /// <summary>
        /// Invoked when the state is entered.
        /// </summary>
        public ProcessCallback OnEnter;

        /// <summary>
        /// Invoked when the state is transitioning to another.
        /// </summary>
        public ProcessStateTransitionCallback OnTransition;

        /// <summary>
        /// Invoked when the state is exited.
        /// </summary>
        public ProcessCallback OnExit;

        /// <summary>
        /// Invoked when the state is entered to start a coroutine.
        /// </summary>
        public ProcessCoroutine Sequence;

        /// <summary>
        /// Invoked when the state is suspended.
        /// </summary>
        public ProcessCallback OnSuspend;

        /// <summary>
        /// Invoked when the state is resumed.
        /// </summary>
        public ProcessCallback OnResume;

        /// <summary>
        /// Invoked when a signal is sent to the process.
        /// </summary>
        public ProcessSignalCallback OnSignal;

        #endregion // Events

        internal void GeneratePhaseMask() {
            GameLoopPhaseMask phases = 0;
            if (OnPreUpdate != null) {
                phases |= GameLoopPhaseMask.PreUpdate;
            }
            if (OnFixedUpdate != null) {
                phases |= GameLoopPhaseMask.FixedUpdate;
            }
            if (OnLateFixedUpdate != null) {
                phases |= GameLoopPhaseMask.LateFixedUpdate;
            }
            if (OnUpdate != null) {
                phases |= GameLoopPhaseMask.Update;
            }
            if (OnUnscaledUpdate != null) {
                phases |= GameLoopPhaseMask.UnscaledUpdate;
            }
            if (OnLateUpdate != null) {
                phases |= GameLoopPhaseMask.LateUpdate;
            }
            if (OnUnscaledLateUpdate != null) {
                phases |= GameLoopPhaseMask.UnscaledLateUpdate;
            }
            UpdatePhaseMask = phases;
        }

        #region Factory

        /// <summary>
        /// Generates a state method table from the static methods of the given type.
        /// </summary>
        static public ProcessStateVTable FromType(Type type) {
            ProcessStateVTable table = default;
            table.OnEnter = FindCallback<ProcessCallback>("OnEnter", type);
            table.OnExit = FindCallback<ProcessCallback>("OnExit", type);
            table.OnTransition = FindCallback<ProcessStateTransitionCallback>("OnTransition", type);
            table.Sequence = FindCallback<ProcessCoroutine>("Sequence", type);
            table.OnSuspend = FindCallback<ProcessCallback>("OnSuspend", type);
            table.OnResume = FindCallback<ProcessCallback>("OnResume", type);
            table.OnSignal = FindCallback<ProcessSignalCallback>("OnSignal", type);
            table.OnPreUpdate = FindCallback<ProcessUpdateCallback>("OnPreUpdate", type);
            table.OnFixedUpdate = FindCallback<ProcessUpdateCallback>("OnFixedUpdate", type);
            table.OnLateFixedUpdate = FindCallback<ProcessUpdateCallback>("OnLateFixedUpdate", type);
            table.OnUpdate = FindCallback<ProcessUpdateCallback>("OnUpdate", type);
            table.OnUnscaledUpdate = FindCallback<ProcessUpdateCallback>("OnUnscaledUpdate", type);
            table.OnLateUpdate = FindCallback<ProcessUpdateCallback>("OnLateUpdate", type);
            table.OnUnscaledLateUpdate = FindCallback<ProcessUpdateCallback>("OnUnscaledLateUpdate", type);
            table.GeneratePhaseMask();
            return table;
        }

        /// <summary>
        /// Generates a state method table from the static methods of the given type.
        /// </summary>
        static public ProcessStateVTable FromTypePrefixed(string prefix, Type type) {
            ProcessStateVTable table = default;
            table.OnEnter = FindCallback<ProcessCallback>(prefix + "OnEnter", type);
            table.OnExit = FindCallback<ProcessCallback>(prefix + "OnExit", type);
            table.OnTransition = FindCallback<ProcessStateTransitionCallback>(prefix + "OnTransition", type);
            table.Sequence = FindCallback<ProcessCoroutine>(prefix + "Sequence", type);
            table.OnSuspend = FindCallback<ProcessCallback>(prefix + "OnSuspend", type);
            table.OnResume = FindCallback<ProcessCallback>(prefix + "OnResume", type);
            table.OnSignal = FindCallback<ProcessSignalCallback>(prefix + "OnSignal", type);
            table.OnPreUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnPreUpdate", type);
            table.OnFixedUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnFixedUpdate", type);
            table.OnLateFixedUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnLateFixedUpdate", type);
            table.OnUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUpdate", type);
            table.OnUnscaledUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUnscaledUpdate", type);
            table.OnLateUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnLateUpdate", type);
            table.OnUnscaledLateUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUnscaledLateUpdate", type);
            table.GeneratePhaseMask();
            return table;
        }

        /// <summary>
        /// Generates a state method table from the instance methods of the given object.
        /// </summary>
        static public ProcessStateVTable FromObject(object target) {
            ProcessStateVTable table = default;
            Type type = target.GetType();
            table.OnEnter = FindCallback<ProcessCallback>("OnEnter", type, target);
            table.OnExit = FindCallback<ProcessCallback>("OnExit", type, target);
            table.OnTransition = FindCallback<ProcessStateTransitionCallback>("OnTransition", type, target);
            table.Sequence = FindCallback<ProcessCoroutine>("Sequence", type, target);
            table.OnSuspend = FindCallback<ProcessCallback>("OnSuspend", type, target);
            table.OnResume = FindCallback<ProcessCallback>("OnResume", type, target);
            table.OnSignal = FindCallback<ProcessSignalCallback>("OnSignal", type, target);
            table.OnPreUpdate = FindCallback<ProcessUpdateCallback>("OnPreUpdate", type, target);
            table.OnFixedUpdate = FindCallback<ProcessUpdateCallback>("OnFixedUpdate", type, target);
            table.OnLateFixedUpdate = FindCallback<ProcessUpdateCallback>("OnLateFixedUpdate", type, target);
            table.OnUpdate = FindCallback<ProcessUpdateCallback>("OnUpdate", type, target);
            table.OnUnscaledUpdate = FindCallback<ProcessUpdateCallback>("OnUnscaledUpdate", type, target);
            table.OnLateUpdate = FindCallback<ProcessUpdateCallback>("OnLateUpdate", type, target);
            table.OnUnscaledLateUpdate = FindCallback<ProcessUpdateCallback>("OnUnscaledLateUpdate", type, target);
            table.GeneratePhaseMask();
            return table;
        }

        /// <summary>
        /// Generates a state method table from the instance methods of the given object.
        /// </summary>
        static public ProcessStateVTable FromObjectPrefixed(string prefix, object target) {
            ProcessStateVTable table = default;
            Type type = target.GetType();
            table.OnEnter = FindCallback<ProcessCallback>(prefix + "OnEnter", type, target);
            table.OnExit = FindCallback<ProcessCallback>(prefix + "OnExit", type, target);
            table.OnTransition = FindCallback<ProcessStateTransitionCallback>(prefix + "OnTransition", type, target);
            table.Sequence = FindCallback<ProcessCoroutine>(prefix + "Sequence", type, target);
            table.OnSuspend = FindCallback<ProcessCallback>(prefix + "OnSuspend", type, target);
            table.OnResume = FindCallback<ProcessCallback>(prefix + "OnResume", type, target);
            table.OnSignal = FindCallback<ProcessSignalCallback>(prefix + "OnSignal", type, target);
            table.OnPreUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnPreUpdate", type, target);
            table.OnFixedUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnFixedUpdate", type, target);
            table.OnLateFixedUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnLateFixedUpdate", type, target);
            table.OnUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUpdate", type, target);
            table.OnUnscaledUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUnscaledUpdate", type, target);
            table.OnLateUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnLateUpdate", type, target);
            table.OnUnscaledLateUpdate = FindCallback<ProcessUpdateCallback>(prefix + "OnUnscaledLateUpdate", type, target);
            table.GeneratePhaseMask();
            return table;
        }

        /// <summary>
        /// Generates a state method table from the given configuration object.
        /// </summary>
        static public ProcessStateVTable FromCallbacks(IProcessStateCallbacks type) {
            Assert.NotNull(type);

            ProcessStateVTable table = default;
            
            IProcessStateEnterExit enterExit = type as IProcessStateEnterExit;
            if (enterExit != null) {
                table.OnEnter = enterExit.OnEnter;
                table.OnExit = enterExit.OnExit;
            }

            IProcessStateOnTransition transition = type as IProcessStateOnTransition;
            if (transition != null) {
                table.OnTransition = transition.OnTransition;
            }

            IProcessStateSequence sequence = type as IProcessStateSequence;
            if (sequence != null) {
                table.Sequence = sequence.Sequence;
            }

            IProcessStateSuspendResume suspendResume = type as IProcessStateSuspendResume;
            if (suspendResume != null) {
                table.OnSuspend = suspendResume.OnSuspend;
                table.OnResume = suspendResume.OnResume;
            }

            IProcessStateSignal signal = type as IProcessStateSignal;
            if (signal != null) {
                table.OnSignal = signal.OnSignal;
            }

            IProcessStatePreUpdate preUpdate = type as IProcessStatePreUpdate;
            if (preUpdate != null) {
                table.OnPreUpdate = preUpdate.OnPreUpdate;
            }

            IProcessStateFixedUpdate fixedUpdate = type as IProcessStateFixedUpdate;
            if (fixedUpdate != null) {
                table.OnFixedUpdate = fixedUpdate.OnFixedUpdate;
            }

            IProcessStateLateFixedUpdate postFixedUpdate = type as IProcessStateLateFixedUpdate;
            if (postFixedUpdate != null) {
                table.OnLateFixedUpdate = postFixedUpdate.OnLateFixedUpdate;
            }

            IProcessStateUpdate update = type as IProcessStateUpdate;
            if (update != null) {
                table.OnUpdate = update.OnUpdate;
            }

            IProcessStateUnscaledUpdate unscaledUpdate = type as IProcessStateUnscaledUpdate;
            if (unscaledUpdate != null) {
                table.OnUnscaledUpdate = unscaledUpdate.OnUnscaledUpdate;
            }

            IProcessStateLateUpdate lateUpdate = type as IProcessStateLateUpdate;
            if (lateUpdate != null) {
                table.OnLateUpdate = lateUpdate.OnLateUpdate;
            }

            IProcessStateUnscaledLateUpdate unscaledLateUpdate = type as IProcessStateUnscaledLateUpdate;
            if (unscaledLateUpdate != null) {
                table.OnUnscaledLateUpdate = unscaledLateUpdate.OnUnscaledLateUpdate;
            }

            table.GeneratePhaseMask();
            return table;
        }

        static private TCallback FindCallback<TCallback>(string name, Type type) where TCallback : Delegate {
            MethodInfo method = type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) {
                return null;
            }
            try {
                return (TCallback) method.CreateDelegate(typeof(TCallback));
            } catch(Exception _) {
                return null;
            }
        }

        static private TCallback FindCallback<TCallback>(string name, Type type, object target) where TCallback : Delegate {
            MethodInfo method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null) {
                return null;
            }
            try {
                return (TCallback) method.CreateDelegate(typeof(TCallback), target);
            } catch {
                return null;
            }
        }

        #endregion // Factory
    }

    #region Interface types

    /// <summary>
    /// Interface for a process state configuration object.
    /// Don't use this directly, use derived interfaces like IProcessStateUpdate, IProcessStateEnterExit, etc.
    /// </summary>
    public interface IProcessStateCallbacks { }

    /// <summary>
    /// Specifies callbacks for entering and exiting a state.
    /// </summary>
    public interface IProcessStateEnterExit : IProcessStateCallbacks {
        void OnEnter(Process process);
        void OnExit(Process process);
    }

    /// <summary>
    /// Specifies callbacks for transitioning to another state.
    /// </summary>
    public interface IProcessStateOnTransition : IProcessStateCallbacks {
        void OnTransition(Process process, ProcessStateDefinition nextState);
    }

    /// <summary>
    /// Specifies callbacks for a coroutine at the start a state.
    /// </summary>
    public interface IProcessStateSequence : IProcessStateCallbacks {
        IEnumerator Sequence(Process process);
    }

    /// <summary>
    /// Specifies callbacks for suspending and resuming a process.
    /// </summary>
    public interface IProcessStateSuspendResume : IProcessStateCallbacks {
        void OnSuspend(Process process);
        void OnResume(Process process);
    }

    /// <summary>
    /// Specifies callbacks for responding to a signal.
    /// </summary>
    public interface IProcessStateSignal : IProcessStateCallbacks {
        void OnSignal(Process process, StringHash32 signalId, object signalArgs);
    }

    /// <summary>
    /// Specifies callbacks for a pre-update function.
    /// </summary>
    public interface IProcessStatePreUpdate : IProcessStateCallbacks {
        void OnPreUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for a fixed update function.
    /// </summary>
    public interface IProcessStateFixedUpdate : IProcessStateCallbacks {
        void OnFixedUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for a post-fixed update function.
    /// </summary>
    public interface IProcessStateLateFixedUpdate : IProcessStateCallbacks {
        void OnLateFixedUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for an update function.
    /// </summary>
    public interface IProcessStateUpdate : IProcessStateCallbacks {
        void OnUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for an unscaled update function.
    /// </summary>
    public interface IProcessStateUnscaledUpdate : IProcessStateCallbacks {
        void OnUnscaledUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for a late update function.
    /// </summary>
    public interface IProcessStateLateUpdate : IProcessStateCallbacks {
        void OnLateUpdate(Process process, float deltaTime);
    }

    /// <summary>
    /// Specifies callbacks for an unscaled late update function.
    /// </summary>
    public interface IProcessStateUnscaledLateUpdate : IProcessStateCallbacks {
        void OnUnscaledLateUpdate(Process process, float deltaTime);
    }

    #endregion // Interface types
}