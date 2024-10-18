using System;
using System.Collections;
using BeauPools;
using BeauUtil;

namespace FieldDay.Processes {
    public sealed class ProcessSignalWait : IEnumerator, IDisposable {
        private const int STATE_UNINITIALIZED = 0;
        private const int STATE_INITIALIZED = 1;
        private const int STATE_TRIGGERED = 2;
        
        private int m_State;
        private StringHash32 m_TargetSignal;
        private Predicate<object> m_Predicate;
        private Process m_Parent;

        private void Initialize(Process process, StringHash32 signalId, Predicate<object> predicate) {
            m_State = STATE_INITIALIZED;
            m_TargetSignal = signalId;
            m_Parent = process;
            m_Predicate = predicate;
        }

        internal void TryHandle(StringHash32 signalId, object signalArg) {
            if (m_State == STATE_INITIALIZED && signalId == m_TargetSignal && (m_Predicate == null || m_Predicate(signalArg))) {
                m_State = STATE_TRIGGERED;
            }
        }

        internal void Shutdown(bool remove) {
            if (m_State != STATE_UNINITIALIZED) {
                m_TargetSignal = null;
                m_Predicate = null;
                m_State = STATE_UNINITIALIZED;

                if (remove) {
                    m_Parent.RemoveSignalWait(this);
                }

                m_Parent = null;
                s_SignalPool.Free(this);
            }
        }

        #region Interfaces

        public void Dispose() {
            Shutdown(true);
        }

        public bool MoveNext() {
            return m_State != STATE_TRIGGERED;
        }

        object IEnumerator.Current { get { return null; } }

        void IEnumerator.Reset() {
            throw new NotSupportedException();
        }

        #endregion // IEnumerators

        #region Pool

        static private DynamicPool<ProcessSignalWait> s_SignalPool = new DynamicPool<ProcessSignalWait>(16, (p) => new ProcessSignalWait());

        static internal ProcessSignalWait Alloc(Process process, StringHash32 signalId, Predicate<object> predicate) {
            ProcessSignalWait waiter = s_SignalPool.Alloc();
            waiter.Initialize(process, signalId, predicate);
            return waiter;
        }

        #endregion // Pool
    }
}