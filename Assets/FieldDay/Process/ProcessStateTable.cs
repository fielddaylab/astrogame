using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Processes {
    /// <summary>
    /// Map of process states.
    /// </summary>
    public sealed class ProcessStateTable {
        private readonly Dictionary<StringHash32, ProcessStateDefinition> m_StateMap;
        private readonly Dictionary<StringHash32, List<ProcessStateTableTransitionData>> m_TransitionMap;
        
        /// <summary>
        /// Parent state table.
        /// If a key is not present in this table, it'll be looked up in the parent table.
        /// </summary>
        public readonly ProcessStateTable Parent;

        /// <summary>
        /// Default state id.
        /// </summary>
        public StringHash32 DefaultId;

        private ProcessStateTable(int inCapacity, ProcessStateTable parent) {
            m_StateMap = new Dictionary<StringHash32, ProcessStateDefinition>(inCapacity, CompareUtils.DefaultEquals<StringHash32>());
            m_TransitionMap = new Dictionary<StringHash32, List<ProcessStateTableTransitionData>>(inCapacity, CompareUtils.DefaultEquals<StringHash32>());
            Parent = parent;
        }

        private ProcessStateTable(Dictionary<StringHash32, ProcessStateDefinition> copyFrom, StringHash32 defaultId) {
            m_StateMap = new Dictionary<StringHash32, ProcessStateDefinition>(copyFrom, CompareUtils.DefaultEquals<StringHash32>());
            m_TransitionMap = new Dictionary<StringHash32, List<ProcessStateTableTransitionData>>(m_StateMap.Count, CompareUtils.DefaultEquals<StringHash32>());
            DefaultId = defaultId;
        }

        #region Accessors

        /// <summary>
        /// Returns the process state definition for the given id.
        /// </summary>
        public ProcessStateDefinition this[StringHash32 id] {
            get { return Get(id); }
        }

        /// <summary>
        /// Returns if a process state with the given id exists in this table.
        /// </summary>
        public bool Has(StringHash32 id) {
            if (m_StateMap.ContainsKey(id)) {
                return true;
            }

            if (Parent != null) {
                return Parent.Has(id);
            }

            return false;
        }

        /// <summary>
        /// Returns the process state definition for the given id.
        /// </summary>
        public ProcessStateDefinition Get(StringHash32 id) {
            ProcessStateDefinition def;
            if (!m_StateMap.TryGetValue(id, out def)) {
                def = Parent?.Get(id);
            }
            if (def == null) {
                Log.Error("[ProcessStateTable] No state with id '{0}' in table", id.ToDebugString());
            }
            return def;
        }

        #endregion // Accessors

        #region Modifiers

        /// <summary>
        /// Sets the state definition for the given id.
        /// </summary>
        public void Add(ProcessStateDefinition def) {
            Assert.NotNull(def);
            m_StateMap[def.Id] = def;
        }

        /// <summary>
        /// Sets the state definition for the given id.
        /// </summary>
        public void AddFrom(StringHash32 id, IProcessStateCallbacks stateDef, BitSet64 customFlags = default) {
            Assert.NotNull(stateDef);
            Add(ProcessStateDefinition.FromCallbacks(id, stateDef, customFlags));
        }

        /// <summary>
        /// Deletes the state definition for the given id.
        /// </summary>
        public void Delete(StringHash32 id) {
            m_StateMap.Remove(id);
        }

        #endregion // Modifiers

        #region Transitions

        /// <summary>
        /// Adds a transition between the given states for the given signal.
        /// </summary>
        public void AddTransition(StringHash32 stateId, StringHash32 targetId, StringHash32 signalId, ProcessStateTransitionPredicate predicate = null, ProcessStateTransitionCallback callback = null) {
            Assert.True(Has(stateId), "State '{0}' not present in state table", stateId);
            List<ProcessStateTableTransitionData> data = GetTransitionData(stateId, true);
            ProcessStateTableTransitionData trans = new ProcessStateTableTransitionData(signalId, targetId, null, predicate, callback);
            data.Add(trans);
        }

        /// <summary>
        /// Adds a transition between the given states for the given signal.
        /// </summary>
        public void AddTransition(StringHash32 stateId, ProcessStateDefinition target, StringHash32 signalId, ProcessStateTransitionPredicate predicate = null, ProcessStateTransitionCallback callback = null) {
            Assert.True(Has(stateId), "State '{0}' not present in state table", stateId);
            Assert.NotNull(target, "Cannot specify a null target");
            List<ProcessStateTableTransitionData> data = GetTransitionData(stateId, true);
            ProcessStateTableTransitionData trans = new ProcessStateTableTransitionData(signalId, null, target, predicate, callback);
            data.Add(trans);
        }

        public void DeleteTransition(StringHash32 stateId, StringHash32 targetId, StringHash32 signalId) {
            // TODO: Implement
        }

        public void DeleteTransition(StringHash32 stateId, ProcessStateDefinition target, StringHash32 signalId) {
            // TODO: Implement
        }

        /// <summary>
        /// Attempts to find a transition from the current state given an input signal.
        /// </summary>
        public bool FindTransition(Process process, StringHash32 signalId, object signalArgs, out ProcessStateTableTransition nextState) {
            List<ProcessStateTableTransitionData> data = GetTransitionData(process.CurrentStateId, false);
            if (data != null) {
                int len = data.Count;
                for (int i = 0; i < len; i++) {
                    ProcessStateTableTransitionData trans = data[i];
                    if (trans.SignalId != signalId) {
                        continue;
                    }

                    ProcessStateDefinition targetDef;
                    if (!trans.IndirectTarget.IsEmpty) {
                        targetDef = Get(trans.IndirectTarget);
                        if (targetDef == null) {
                            continue;
                        }
                    } else {
                        targetDef = trans.DirectTarget;
                    }

                    if (trans.Predicate != null && !trans.Predicate(process, targetDef, signalId, signalArgs)) {
                        continue;
                    }

                    nextState = new ProcessStateTableTransition(targetDef, trans.Callback);
                    return true;
                }
            }

            if (Parent != null) {
                return Parent.FindTransition(process, signalId, signalArgs, out nextState);
            }

            nextState = default;
            return false;
        }

        private List<ProcessStateTableTransitionData> GetTransitionData(StringHash32 stateId, bool create) {
            List<ProcessStateTableTransitionData> data;
            if (!m_TransitionMap.TryGetValue(stateId, out data) && create) {
                data = new List<ProcessStateTableTransitionData>(4);
                m_TransitionMap.Add(stateId, data);
            }
            return data;
        }

        #endregion // Transitions

        #region Factory

        /// <summary>
        /// Creates a new process state table with the given capacity.
        /// </summary>
        static public ProcessStateTable Create(int capacity = 8) {
            return new ProcessStateTable(capacity, null);
        }

        /// <summary>
        /// Creates a new process state table that can override the given parent table.
        /// </summary>
        static public ProcessStateTable Override(ProcessStateTable parent, int capacity = 8) {
            return new ProcessStateTable(capacity, parent);
        }

        /// <summary>
        /// Clones the given process state table.
        /// </summary>
        static public ProcessStateTable Clone(ProcessStateTable source) {
            return new ProcessStateTable(source.m_StateMap, source.DefaultId);
        }

        #endregion // Factor
    }

    /// <summary>
    /// Data for evaluating a state transition.
    /// </summary>
    internal struct ProcessStateTableTransitionData {
        public readonly StringHash32 SignalId;
        public readonly StringHash32 IndirectTarget;
        public readonly ProcessStateDefinition DirectTarget;
        public readonly ProcessStateTransitionPredicate Predicate;
        public readonly ProcessStateTransitionCallback Callback;

        public ProcessStateTableTransitionData(StringHash32 signalId, StringHash32 indirectTarget, ProcessStateDefinition directTarget, ProcessStateTransitionPredicate predicate, ProcessStateTransitionCallback callback) {
            SignalId = signalId;
            IndirectTarget = indirectTarget;
            DirectTarget = directTarget;
            Predicate = predicate;
            Callback = callback;
        }
    }

    /// <summary>
    /// Data for a transition to another state.
    /// </summary>
    public struct ProcessStateTableTransition {
        public readonly ProcessStateDefinition Target;
        public readonly ProcessStateTransitionCallback Callback;

        internal ProcessStateTableTransition(ProcessStateDefinition target, ProcessStateTransitionCallback callback) {
            Target = target;
            Callback = callback;
        }
    }

    /// <summary>
    /// Predicate for a ProcessStateTable signal transition.
    /// </summary>
    public delegate bool ProcessStateTransitionPredicate(Process process, ProcessStateDefinition targetState, StringHash32 signalId, object signalArgs);
}