using BeauUtil;
using System.Collections.Generic;
using BeauPools;
using BeauUtil.Debugger;
using System;

namespace FieldDay.Processes {
    /// <summary>
    /// Process manager. Updates process, maintains update buckets.
    /// </summary>
    public sealed class ProcessMgr {
        private readonly UniqueIdMap32<Process> m_ProcessIdTable = new UniqueIdMap32<Process>(256);
        private PhaseBuckets<Process> m_ProcessUpdates = new PhaseBuckets<Process>(16);
        private readonly HashSet<Process> m_AllProcesses = new HashSet<Process>(32);
        private readonly HashSet<Process> m_ProcessesWithHosts = new HashSet<Process>(32);
        private readonly RingBuffer<Process> m_DirtyProcessQueue = new RingBuffer<Process>(16, RingBufferMode.Expand);
        private readonly RingBuffer<Process> m_PendingProcessQueue = new RingBuffer<Process>(32, RingBufferMode.Expand);
        private readonly DynamicPool<Process> m_ProcessPool;

        internal ProcessMgr() {
            m_ProcessPool = new DynamicPool<Process>(32, (p) => new Process(this));
            m_ProcessPool.Prewarm(16);
        }

        #region Spawning

        /// <summary>
        /// Spawns a new process with the given host, context, and initial state.
        /// </summary>
        public ProcessId Spawn(ProcessStateDefinition initialState, IProcessContext context = null) {
            return Spawn(initialState, context as UnityEngine.Object, context, default);
        }

        /// <summary>
        /// Spawns a new process with the given host, context, and table.
        /// </summary>
        public ProcessId Spawn(ProcessStateTable table, IProcessContext context = null) {
            Assert.NotNull(table);
            return Spawn(table, table[table.DefaultId], context as UnityEngine.Object, context, default);
        }

        /// <summary>
        /// Spawns a new process with the given host, context, table, and initial state.
        /// </summary>
        public ProcessId Spawn(ProcessStateTable table, StringHash32 stateId, IProcessContext context = null) {
            Assert.NotNull(table);
            return Spawn(table, table[stateId], context as UnityEngine.Object, context, default);
        }

        /// <summary>
        /// Spawns a new process with the given name, host, context, and initial state.
        /// </summary>
        public ProcessId Spawn(ProcessStateDefinition initialState, UnityEngine.Object host, IProcessContext context, StringHash32 name = default) {
            Assert.True(initialState != null, "Initial state must not be empty");

            Process process = m_ProcessPool.Alloc();
            ProcessId id = new ProcessId(m_ProcessIdTable.Add(process));
            process.Initialize(id, name, host, context ?? host as IProcessContext, initialState, null);
            m_PendingProcessQueue.PushBack(process);
            if (host != null) {
                m_ProcessesWithHosts.Add(process);
            }
            m_AllProcesses.Add(process);
            return id;
        }

        /// <summary>
        /// Spawns a new process with the given name, host, context, and table, initial state.
        /// </summary>
        public ProcessId Spawn(ProcessStateTable table, ProcessStateDefinition initialState, UnityEngine.Object host, IProcessContext context, StringHash32 name = default) {
            Assert.True(initialState != null, "Initial state must not be empty");

            Process process = m_ProcessPool.Alloc();
            ProcessId id = new ProcessId(m_ProcessIdTable.Add(process));
            process.Initialize(id, name, host, context ?? host as IProcessContext, initialState, table);
            m_PendingProcessQueue.PushBack(process);
            if (host != null) {
                m_ProcessesWithHosts.Add(process);
            }
            m_AllProcesses.Add(process);
            return id;
        }

        #endregion // Spawning

        #region Lookups

        /// <summary>
        /// Returns if an process with the given handle exists.
        /// </summary>
        public bool Has(ProcessId id) {
            return m_ProcessIdTable.Contains(id.m_Id);
        }

        /// <summary>
        /// Retrieves the process for the given identifier.
        /// </summary>
        public Process Get(ProcessId id) {
            return m_ProcessIdTable.Get(id.m_Id);
        }

        #endregion // Lookups

        #region Events

        internal void DebugUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.DebugUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnPreUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void PreUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach(var process in m_ProcessUpdates[GameLoopPhase.PreUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnPreUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void FixedUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.FixedUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnFixedUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void LateFixedUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.LateFixedUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnLateFixedUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void Update(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.Update]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void UnscaledUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.UnscaledUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnUnscaledUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void LateUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.LateUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnLateUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void UnscaledLateUpdate(float deltaTime, int categoryMask) {
            ProcessPending(categoryMask);
            foreach (var process in m_ProcessUpdates[GameLoopPhase.UnscaledLateUpdate]) {
                if (process.ShouldUpdate(categoryMask)) {
                    process.OnUnscaledLateUpdate(deltaTime);
                }
            }
            ProcessDirty();
        }

        internal void FrameAdvanced() {
            HandleDestroyedHosts();
        }

        internal void Shutdown() {
            m_DirtyProcessQueue.Clear();
            m_PendingProcessQueue.Clear();
            foreach(var process in m_AllProcesses) {
                KillProcess(process, false);
            }
            m_AllProcesses.Clear();
            m_ProcessesWithHosts.Clear();
            m_ProcessUpdates.Clear();
            m_ProcessIdTable.Clear();
            m_ProcessPool.Clear();
        }

        #endregion // Events

        #region Dirty

        internal void MarkDirty(Process process) {
            m_DirtyProcessQueue.PushBack(process);
        }

        private void ProcessDirty() {
            int dirty = m_DirtyProcessQueue.Count;
            Process p;
            while(dirty-- > 0 && m_DirtyProcessQueue.TryPopFront(out p)) {
                if (p.IsPendingKill()) {
                    KillProcess(p, true);
                } else {
                    bool suspended = p.IsSuspended();
                    if (suspended != p.m_LastSuspendedState) {
                        p.m_LastSuspendedState = suspended;
                        if (suspended) {
                            p.OnSuspend();
                        } else {
                            p.OnResume();
                        }
                    }

                    if (!suspended) {
                        if (p.ProcessTransitions()) {
                            SwitchBuckets(p); // ensure buckets are up to date
                        }
                    }

                    p.ClearDirty();
                }
            }
        }

        private void ProcessPending(int categoryMask) {
            int pending = m_PendingProcessQueue.Count;
            Process p;
            while(pending-- > 0 && m_PendingProcessQueue.TryPopFront(out p)) {
                if (p.IsPendingKill() || p.HostIsDead()) {
                    KillProcess(p, true);
                } else if (!p.ShouldUpdate(categoryMask)) {
                    m_PendingProcessQueue.PushBack(p);
                } else {
                    p.FinishInitialize();
                    SwitchBuckets(p);
                }
            }
        }

        private void SwitchBuckets(Process process) {
            GameLoopPhaseMask current = process.m_RegisteredMask;
            GameLoopPhaseMask requested = process.m_MethodTable.UpdatePhaseMask;
            
            PhaseBuckets.SwitchBuckets(ref m_ProcessUpdates, process, ref process.m_RegisteredMask, requested);
        }

        private void KillProcess(Process process, bool deregister) {
            GameLoopPhaseMask currentRegistered = process.m_RegisteredMask;
            ProcessId id = process.Id;
            process.Shutdown();
            if (deregister) {
                m_ProcessIdTable.Remove(id.m_Id);
                m_AllProcesses.Remove(process);
                PhaseBuckets.SwitchBuckets(ref m_ProcessUpdates, process, ref currentRegistered, 0);
                m_ProcessPool.Free(process);
            }
        }

        private void HandleDestroyedHosts() {
            m_ProcessesWithHosts.RemoveWhere(m_HostDeadHandler ?? (m_HostDeadHandler = (p) => {
                if (p.HostIsDead()) {
                    KillProcess(p, true);
                    return true;
                }
                return false;
            }));
        }

        private Predicate<Process> m_HostDeadHandler;

        #endregion // Dirty
    }
}