using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Processes {
    /// <summary>
    /// Updatable process with an internal state machine.
    /// </summary>
    public sealed class Process {
        internal ProcessStateVTable m_MethodTable;
        internal GameLoopPhaseMask m_RegisteredMask;
        internal bool m_LastSuspendedState;
        private int m_CategoryMask;
        private ProcessStatus m_Status;
        private ProcessRuntimeFlags m_Flags;
        private IProcessContext m_Context;
        private RawStateBlock256 m_StateDataBlock;
        private float m_TimeScale = 1;

        // host information
        private UnityEngine.Object m_Host;
        private Behaviour m_HostBehaviour;
        private GameObject m_HostGO;

        // less frequently used
        private ProcessId m_Id;
        private StringHash32 m_Name;
        private Routine m_Sequence;
        private readonly ProcessMgr m_Mgr;
        private RingBuffer<ProcessSignalWait> m_Waits;

        // callbacks
        public event ProcessEnterExitCallback OnStateEnter;
        public event ProcessTransitionCallback OnStateTransition;
        public event ProcessEnterExitCallback OnStateExit;

        // metadata
        private ProcessStateTable m_Table;
        private ProcessStateDefinition m_DefaultState;
        private ProcessStateDefinition m_CurrentState;
        private ProcessStateDefinition m_NextState;

        internal Process(ProcessMgr mgr) {
            m_Mgr = mgr;
            m_Waits = new RingBuffer<ProcessSignalWait>(4);
        }

        #region Properties

        /// <summary>
        /// Id of this process.
        /// </summary>
        public ProcessId Id {
            get { return m_Id; }
        }

        /// <summary>
        /// Custom name of the Process.
        /// </summary>
        public StringHash32 Name {
            get { return m_Name; }
            set { m_Name = value; }
        }

        /// <summary>
        /// Gets/sets the custom update mask.
        /// </summary>
        public int CategoryMask {
            get { return m_CategoryMask; }
            set { m_CategoryMask = value; }
        }

        /// <summary>
        /// Gets/sets the timescale.
        /// </summary>
        public float TimeScale {
            get { return m_TimeScale; }
            set { m_TimeScale = value; }
        }

        #endregion // Properties

        #region States

        /// <summary>
        /// Returns the id of the current state.
        /// </summary>
        public StringHash32 CurrentStateId {
            get { return m_CurrentState?.Id ?? StringHash32.Null; }
        }

        /// <summary>
        /// Returns the current state definition.
        /// </summary>
        public ProcessStateDefinition CurrentState {
            get { return m_CurrentState; }
        }

        /// <summary>
        /// Returns the source of the current state.
        /// </summary>
        public TStateSource CurrentStateObject<TStateSource>() where TStateSource : class {
            return m_CurrentState?.Target as TStateSource;
        }

        /// <summary>
        /// Returns the assigned process state table.
        /// </summary>
        public ProcessStateTable Table {
            get { return m_Table; }
        }

        #endregion // States

        #region Host

        /// <summary>
        /// Host linked to this process.
        /// </summary>
        public UnityEngine.Object Host {
            get { return m_Host; }
        }

        /// <summary>
        /// Host GameObject linked to this process.
        /// </summary>
        public GameObject HostGO {
            get { return m_HostGO; }
        }

        /// <summary>
        /// Host MonoBehaviour linked to this process.
        /// </summary>
        public MonoBehaviour HostBehaviour {
            get { return m_HostBehaviour as MonoBehaviour; }
        }

        #endregion // Host

        #region Context

        /// <summary>
        /// Returns the process context.
        /// </summary>
        public TContext Context<TContext>() where TContext : IProcessContext {
            return (TContext) m_Context;
        }

        /// <summary>
        /// Returns the process data storage block, casted as the given type.
        /// </summary>
        public ref TData Data<TData>() where TData : unmanaged {
            return ref m_StateDataBlock.Ref<TData>();
        }

        /// <summary>
        /// Updates the process data storage block with the given data.
        /// </summary>
        public void SetData<TData>(in TData data) where TData : unmanaged {
            m_StateDataBlock.Store(data);
        }

        #endregion // Context

        #region Status

        internal bool IsInitialized() {
            return (m_Status & ProcessStatus.Initialized) != 0;
        }

        /// <summary>
        /// Returns if the process is suspended.
        /// </summary>
        public bool IsSuspended() {
            return (m_Status & ProcessStatus.AnySuspended) != 0;
        }

        internal bool IsUpdating() {
            return (m_Status & ProcessStatus.Updating) != 0;
        }

        /// <summary>
        /// Returns if the process is pending to transition to another state.
        /// </summary>
        public bool IsPendingTransition() {
            return m_NextState != null;
        }

        /// <summary>
        /// Returns if the process is pending to be killed.
        /// </summary>
        public bool IsPendingKill() {
            return (m_Status & ProcessStatus.PendingKill) != 0;
        }

        internal bool IsDirty() {
            return (m_Status & ProcessStatus.PendingChanges) != 0;
        }

        #endregion // Status

        #region Events

        internal void Initialize(ProcessId id, StringHash32 name, UnityEngine.Object host, IProcessContext context, ProcessStateDefinition initialState, ProcessStateTable stateTable) {
            m_Id = id;
            m_Name = name;
            m_Host = host;
            m_Context = context;
            m_DefaultState = initialState;
            m_CategoryMask = Bits.All32;
            m_Table = stateTable;

            m_HostBehaviour = host as Behaviour;
            if (host is GameObject) {
                m_HostGO = (GameObject) host;
            } else if (host is Component) {
                m_HostGO = ((Component) host).gameObject;
            } else {
                m_HostGO = null;
            }
        }

        internal void FinishInitialize() {
            if (!ProcessTransitions()) {
                m_CurrentState = m_DefaultState;
                EnterCurrentState();
            }

            m_Status |= ProcessStatus.Initialized;
        }

        internal void OnPreUpdate(float deltaTime) {
            Assert.True(((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.PreUpdate) != 0));
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnPreUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnFixedUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.FixedUpdate) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnFixedUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnLateFixedUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.LateFixedUpdate) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnLateFixedUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.Update) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnUnscaledUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.UnscaledUpdate) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnUnscaledUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnLateUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.LateUpdate) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnLateUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnUnscaledLateUpdate(float deltaTime) {
            Assert.True((m_MethodTable.UpdatePhaseMask & GameLoopPhaseMask.UnscaledLateUpdate) != 0);
            m_Status |= ProcessStatus.Updating;
            m_MethodTable.OnUnscaledLateUpdate(this, deltaTime * m_TimeScale);
            m_Status &= ~ProcessStatus.Updating;
        }

        internal void OnSuspend() {
            m_MethodTable.OnSuspend?.Invoke(this);
            m_Sequence.Pause();
        }
        
        internal void OnResume() {
            m_MethodTable.OnResume?.Invoke(this);
            m_Sequence.Resume();
        }

        internal bool ShouldUpdate(int updateMask) {
            return (m_Status & (ProcessStatus.AnySuspended | ProcessStatus.PendingKill | ProcessStatus.PendingChanges)) == 0 && (m_CategoryMask & updateMask) != 0 && ((m_Flags & ProcessRuntimeFlags.IgnoreHostActive) != 0 || HostIsActive());
        }

        internal bool HostIsActive() {
            if (!ReferenceEquals(m_HostBehaviour, null)) {
                return m_HostBehaviour.isActiveAndEnabled;
            }
            if (!ReferenceEquals(m_HostGO, null)) {
                return m_HostGO.activeInHierarchy;
            }
            return true;
        }

        internal bool HostIsDead() {
            return !ReferenceEquals(m_Host, null) && !m_Host;
        }

        internal void Shutdown() {
            ExitCurrentState(null);
            while(m_Waits.TryPopFront(out ProcessSignalWait wait)) {
                wait.Shutdown(false);
            }
            m_NextState = null;
            m_DefaultState = null;
            m_CategoryMask = 0;
            m_RegisteredMask = 0;
            m_Flags = 0;
            m_Host = null;
            m_HostGO = null;
            m_HostBehaviour = null;
            m_Context = null;
            m_StateDataBlock = default;
            m_Id = default;
            m_Name = default;
            m_Status = 0;
            m_LastSuspendedState = false;
            m_TimeScale = 1;
            OnStateEnter = null;
            OnStateEnter = null;
            OnStateTransition = null;
            m_Table = null;
        }

        #endregion // Events

        #region Transitions

        /// <summary>
        /// Queues the next state.
        /// </summary>
        public void TransitionTo(ProcessStateDefinition newState) {
            if (m_NextState != newState) {
                m_NextState = newState;
                MarkDirty();
            }
        }

        /// <summary>
        /// Queues the next state.
        /// </summary>
        public void TransitionTo<TArg>(ProcessStateDefinition newState, in TArg nextStateData) where TArg : unmanaged {
            if (m_NextState != newState) {
                m_NextState = newState;
                SetData(nextStateData);
                MarkDirty();
            }
        }

        /// <summary>
        /// Queues the next state from the state table.
        /// </summary>
        public void TransitionTo(StringHash32 id) {
            Assert.NotNull(m_Table);
            TransitionTo(m_Table[id]);
        }

        /// <summary>
        /// Queues a return to the initial state.
        /// </summary>
        public void TransitionToDefault() {
            TransitionTo(m_DefaultState);
        }

        /// <summary>
        /// Suspends the current state.
        /// </summary>
        public void Suspend() {
            if ((m_Status & ProcessStatus.Suspended_Self) == 0) {
                m_Status |= ProcessStatus.Suspended_Self;
                MarkDirty();
            }
        }

        /// <summary>
        /// Suspends the current state.
        /// </summary>
        public void Resume() {
            if ((m_Status & ProcessStatus.Suspended_Self) != 0) {
                m_Status &= ~ProcessStatus.Suspended_Self;
                MarkDirty();
            }
        }

        /// <summary>
        /// Marks the process to be killed.
        /// </summary>
        public void Kill() {
            if ((m_Status & ProcessStatus.PendingKill) == 0) {
                m_Status |= ProcessStatus.PendingKill;
                MarkDirty();
            }
        }

        /// <summary>
        /// Processes any queued transitions.
        /// </summary>
        internal bool ProcessTransitions() {
            bool changed = m_NextState != null;
            while(m_NextState != null) {
                ExitCurrentState(m_NextState);
                m_CurrentState = m_NextState;
                m_NextState = null;
                EnterCurrentState();
            }
            return changed;
        }

        /// <summary>
        /// Ends the current state.
        /// </summary>
        internal void ExitCurrentState(ProcessStateDefinition next) {
            if (next != null) {
                OnStateTransition?.Invoke(this, m_CurrentState, m_NextState);
                m_MethodTable.OnTransition?.Invoke(this, next);
            }
            m_MethodTable.OnExit?.Invoke(this);
            OnStateExit?.Invoke(this, m_CurrentState);
            m_MethodTable = default;
            m_CurrentState = null;
            m_Sequence.Stop();
        }

        /// <summary>
        /// Begins the current state and starts its associated coroutine, if defined.
        /// </summary>
        internal void EnterCurrentState() {
            if (m_CurrentState != null) {
                m_MethodTable = m_CurrentState.Table;
                m_MethodTable.OnEnter?.Invoke(this);
                if (m_NextState == null && m_MethodTable.Sequence != null) {
                    m_Sequence.Replace(HostBehaviour, m_MethodTable.Sequence(this));
                }
                OnStateEnter?.Invoke(this, m_CurrentState);
            }
        }

        private void MarkDirty() {
            if ((m_Status & ProcessStatus.Initialized) != 0 && (m_Status & ProcessStatus.PendingChanges) == 0) {
                m_Status |= ProcessStatus.PendingChanges;
                m_Mgr.MarkDirty(this);
            }
        }

        internal void ClearDirty() {
            m_Status &= ~ProcessStatus.PendingChanges;
        }

        #endregion // Transitions

        #region Signals

        /// <summary>
        /// Sends a signal to the current process state.
        /// Will also poll the current table for a state transition based on the input signal.
        /// </summary>
        public void Signal(StringHash32 signalId, object signalArgs = null) {
            ProcessSignalWaiters(signalId, signalArgs);
            m_MethodTable.OnSignal?.Invoke(this, signalId, signalArgs);
            if (m_Table != null && !IsPendingTransition() && !IsPendingKill()) {
                bool hasTransition = m_Table.FindTransition(this, signalId, signalArgs, out ProcessStateTableTransition trans);
                if (hasTransition) {
                    TransitionTo(trans.Target);
                    trans.Callback?.Invoke(this, trans.Target);
                }
            }
        }

        private void ProcessSignalWaiters(StringHash32 signalId, object signalArg) {
            for(int i = m_Waits.Count - 1; i >= 0; i--) {
                m_Waits[i].TryHandle(signalId, signalArg);
            }
        }

        /// <summary>
        /// Creates an IEnumerator that Waits for the process to receive the given signal.
        /// </summary>
        public ProcessSignalWait WaitForSignal(StringHash32 signalId) {
            ProcessSignalWait wait = ProcessSignalWait.Alloc(this, signalId, null);
            m_Waits.PushBack(wait);
            return wait;
        }

        /// <summary>
        /// Creates an IEnumerator that Waits for the process to receive the given signal.
        /// </summary>
        public ProcessSignalWait WaitForSignal(StringHash32 signalId, Predicate<object> signalArgPredicate) {
            ProcessSignalWait wait = ProcessSignalWait.Alloc(this, signalId, signalArgPredicate);
            m_Waits.PushBack(wait);
            return wait;
        }

        internal void RemoveSignalWait(ProcessSignalWait wait) {
            m_Waits.FastRemove(wait);
        }

        #endregion // Signals
    }

    public delegate void ProcessEnterExitCallback(Process process, ProcessStateDefinition state);
    public delegate void ProcessTransitionCallback(Process process, ProcessStateDefinition current, ProcessStateDefinition next);

    [Flags]
    internal enum ProcessRuntimeFlags : uint {
        IgnoreHostActive = 0x01,
    }

    [Flags]
    internal enum ProcessStatus : uint {
        Initialized = 0x01,
        Suspended_Self = 0x02,
        Suspended_AsChild = 0x04,
        PendingKill = 0x08,
        Updating = 0x10,
        PendingChanges = 0x20,
        Suspended_SignalWait = 0x40,

        AnySuspended = Suspended_Self | Suspended_AsChild | Suspended_SignalWait
    }

    /// <summary>
    /// Interface for process context data.
    /// </summary>
    public interface IProcessContext { }

    /// <summary>
    /// Unique process identifier.
    /// </summary>
    [DefaultEqualityComparer(typeof(ProcessId.Comparer)), DefaultSorter(typeof(ProcessId.Comparer))]
    public struct ProcessId : IEquatable<ProcessId>, IComparable<ProcessId> {
        internal readonly UniqueId32 m_Id;

        internal ProcessId(UniqueId32 id) {
            m_Id = id;
        }

        /// <summary>
        /// Null process.
        /// </summary>
        static public readonly ProcessId Null = default(ProcessId);

        #region Shortcuts

        /// <summary>
        /// Returns if this id points to a valid process.
        /// </summary
        public bool IsAlive() {
            return Game.Processes.Has(this);
        }

        /// <summary>
        /// Returns if this id once pointed to a valid process, but the process has since been destroyed.
        /// </summary>
        public bool WasDestroyed() {
            return m_Id.Id != 0 && !Game.Processes.Has(this);
        }

        /// <summary>
        /// Returns the process associated with this id.
        /// </summary>
        public Process Process {
            get { return Game.Processes.Get(this); }
        }

        /// <summary>
        /// Transitions to the given state.
        /// </summary>
        public void TransitionTo(ProcessStateDefinition newState) {
            Process?.TransitionTo(newState);
        }

        /// <summary>
        /// Transitions to the given state, with the given argument.
        /// </summary>
        public void TransitionTo<TArg>(ProcessStateDefinition newState, in TArg nextStateArg) where TArg : unmanaged {
            Process?.TransitionTo(newState, nextStateArg);
        }

        /// <summary>
        /// Sets the data for this process.
        /// </summary>
        public void SetData<TArg>(in TArg data) where TArg : unmanaged {
            Process?.SetData(data);
        }

        /// <summary>
        /// Returns the id of the current state.
        /// </summary>
        public StringHash32 CurrentStateId {
            get { return Process?.CurrentStateId ?? StringHash32.Null; }
        }

        /// <summary>
        /// Attempts to get data for this process.
        /// </summary>
        public bool TryGetData<TArg>(out TArg outData) where TArg : unmanaged {
            Process p = Process;
            if (p != null) {
                outData = p.Data<TArg>();
                return true;
            }

            outData = default;
            return false;
        }

        /// <summary>
        /// Kills this process.
        /// </summary>
        public void Kill() {
            Process p = Game.Processes?.Get(this);
            p?.Kill();
        }

        /// <summary>
        /// Sends a signal to this process.
        /// </summary>
        public void Signal(StringHash32 signalId, object signalArgs = null) {
            Game.Processes?.Get(this)?.Signal(signalId, signalArgs);
        }

        #endregion // Shortcuts

        #region Interfaces

        public bool Equals(ProcessId other) {
            return m_Id == other.m_Id;
        }

        public int CompareTo(ProcessId other) {
            return m_Id.CompareTo(other.m_Id);
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is ProcessId)
                return Equals((ProcessId) obj);
            return false;
        }

        public override int GetHashCode() {
            return m_Id.GetHashCode();
        }

        public override string ToString() {
            return m_Id.ToString();
        }

        #endregion // Overrides

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator ==(ProcessId inA, ProcessId inB) {
            return inA.m_Id == inB.m_Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator !=(ProcessId inA, ProcessId inB) {
            return inA.m_Id != inB.m_Id;
        }

        static public bool operator <(ProcessId inA, ProcessId inB) {
            return inA.m_Id < inB.m_Id;
        }

        static public bool operator <=(ProcessId inA, ProcessId inB) {
            return inA.m_Id <= inB.m_Id;
        }

        static public bool operator >(ProcessId inA, ProcessId inB) {
            return inA.m_Id > inB.m_Id;
        }

        static public bool operator >=(ProcessId inA, ProcessId inB) {
            return inA.m_Id >= inB.m_Id;
        }

        #endregion // Operators

        #region Comparisons

        /// <summary>
        /// Default comparer.
        /// </summary>
        private sealed class Comparer : IEqualityComparer<ProcessId>, IComparer<ProcessId> {
            public int Compare(ProcessId x, ProcessId y) {
                return x.m_Id.CompareTo(y.m_Id);
            }

            public bool Equals(ProcessId x, ProcessId y) {
                return x.m_Id == y.m_Id;
            }

            public int GetHashCode(ProcessId obj) {
                return obj.m_Id.GetHashCode();
            }
        }

        #endregion // Comparisons
    }
}