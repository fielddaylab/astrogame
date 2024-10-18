using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Processes {
    /// <summary>
    /// MonoBehaviour with shortcuts for hosting and providing context for a process.
    /// </summary>
    public class ProcessBehaviour : MonoBehaviour, IProcessContext {
        /// <summary>
        /// State table.
        /// </summary>
        public ProcessStateTable StateTable;

        /// <summary>
        /// Main process identifier.
        /// </summary>
        protected ProcessId m_MainProcess;

        /// <summary>
        /// Is this part of a pool.
        /// </summary>
        private bool m_FromPool;

        #region Events

        protected virtual void Start() {

        }

        protected virtual void OnDestroy() {
            m_MainProcess.Kill();
        }

        #endregion // Events

        /// <summary>
        /// Main process.
        /// </summary>
        public ProcessId MainProcess {
            get { return m_MainProcess; }
        }

        #region Start

        /// <summary>
        /// Starts a process with the given initial state, using this behavior as host and context.
        /// </summary>
        public ProcessId StartProcess(ProcessStateDefinition initialState, StringHash32 name = default) {
            return Game.Processes.Spawn(StateTable, initialState, this, this, name);
        }

        /// <summary>
        /// Starts/replaces this behavior's main process with the given initial state, using this behavior as host and context.
        /// </summary>
        public ProcessId StartMainProcess(ProcessStateDefinition initialState, StringHash32 name = default) {
            m_MainProcess.Kill();
            return (m_MainProcess = StartProcess(initialState, name));
        }

        /// <summary>
        /// Starts/replaces this behavior's main process with initial state of the current state table, using this behavior as host and context.
        /// </summary>
        public ProcessId StartMainProcess(StringHash32 name = default) {
            m_MainProcess.Kill();
            Assert.NotNull(StateTable);
            return (m_MainProcess = StartProcess(StateTable[StateTable.DefaultId], name));
        }

        /// <summary>
        /// Starts/replaces this behavior's main process with the given initial state from the current state table, using this behavior as host and context.
        /// </summary>
        public ProcessId StartMainProcess(StringHash32 stateId, StringHash32 name = default) {
            m_MainProcess.Kill();
            Assert.NotNull(StateTable);
            return (m_MainProcess = StartProcess(StateTable[stateId], name));
        }

        #endregion // Start

        #region Signals

        /// <summary>
        /// Sends a signal to the current main process.
        /// </summary
        public virtual void Signal(StringHash32 signalId, object signalArgs = null) {
            m_MainProcess.Signal(signalId, signalArgs);
        }

        #endregion // Signals
    }
}