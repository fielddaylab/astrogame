#if !UNITY_WEBGL
#define SUPPORTS_THREADING
#endif // !UNITY_WEBGL

#if UNITY_EDITOR
#define DEADLOCK_DETECTION
#endif // UNITY_EDITOR

using BeauUtil;
using BeauUtil.Debugger;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FieldDay.Threading {
    public struct AtomicRWLock {
        [NonSerialized] internal long m_Value;
        
        internal const long WriteValue = unchecked((long) 0xF00D99995555BEEF);

        #region Read

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireRead() {
            Atomics.AcquireRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAcquireRead() {
            return Atomics.TryAcquireRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseRead() {
            Atomics.ReleaseRead(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAcquireRead() {
            return Atomics.CanAcquireRead(ref this);
        }

        #endregion // Read

        #region Write

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquireWrite() {
            Atomics.AcquireWrite(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseWrite() {
            Atomics.ReleaseWrite(ref this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanAcquireWrite() {
            return Atomics.CanAcquireWrite(ref this);
        }

        #endregion // Write
    }

    static public class Atomics {
        #region Read

        static public void AcquireRead(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING

#if DEADLOCK_DETECTION
            long stopAfter = Stopwatch.GetTimestamp() + TimeSpan.TicksPerSecond;
#endif // DEADLOCK_DETECTION

            long val;
            while ((val = Interlocked.Read(ref atomic.m_Value)) == AtomicRWLock.WriteValue || Interlocked.CompareExchange(ref atomic.m_Value, val + 1, val) != val) {
#if DEADLOCK_DETECTION
                if (Stopwatch.GetTimestamp() > stopAfter) {
                    throw new DeadlockException("read deadlock");
                }
#endif // DEADLOCK_DETECTION
            }
#else
            Assert.True(atomic.m_Value != AtomicRWLock.WriteValue);
            atomic.m_Value++;
#endif // SUPPORTS_THREADING
        }

        static public bool TryAcquireRead(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING
            long val;
            if ((val = Interlocked.Read(ref atomic.m_Value)) == AtomicRWLock.WriteValue || Interlocked.CompareExchange(ref atomic.m_Value, val + 1, val) != val)
                return false;

            return true;
#else
            if (atomic.m_Value == AtomicRWLock.WriteValue) {
                return false;
            }

            atomic.m_Value++;
            return true;
#endif // SUPPORTS_THREADING
        }

        static public void ReleaseRead(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING
            Assert.True(Interlocked.Read(ref atomic.m_Value) > 0, "Atomics.ReleaseRead unbalanced");
            Interlocked.Decrement(ref atomic.m_Value);
#else
            Assert.True(atomic.m_Value > 0, "Atomics.ReleaseRead unbalanced");
            atomic.m_Value--;
#endif // SUPPORTS_THREADING
        }

        static public bool CanAcquireRead(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING
            return Interlocked.Read(ref atomic.m_Value) != AtomicRWLock.WriteValue;
#else
            return atomic.m_Value != AtomicRWLock.WriteValue;
#endif // SUPPORTS_THREADING
        }

        #endregion // Read

        #region Write

        static public void AcquireWrite(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING

#if DEADLOCK_DETECTION
            long stopAfter = Stopwatch.GetTimestamp() + TimeSpan.TicksPerSecond;
#endif // DEADLOCK_DETECTION

            while (Interlocked.CompareExchange(ref atomic.m_Value, AtomicRWLock.WriteValue, 0) != 0) {
#if DEADLOCK_DETECTION
                if (Stopwatch.GetTimestamp() > stopAfter) {
                    throw new DeadlockException("write deadlock");
                }
#endif // DEADLOCK_DETECTION
            }
#else
            Assert.True(atomic.m_Value == 0);
            atomic.m_Value = AtomicRWLock.WriteValue;
#endif // SUPPORTS_THREADING
        }

        static public void ReleaseWrite(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING
            Assert.True(Interlocked.Read(ref atomic.m_Value) == AtomicRWLock.WriteValue, "Atomics.ReleaseWrite unbalanced");
            Interlocked.Exchange(ref atomic.m_Value, 0);
#else
            Assert.True(atomic.m_Value == AtomicRWLock.WriteValue, "Atomics.ReleaseWrite unbalanced");
            atomic.m_Value = 0;
#endif // SUPPORTS_THREADING
        }

        static public bool CanAcquireWrite(ref AtomicRWLock atomic) {
#if SUPPORTS_THREADING
            return Interlocked.Read(ref atomic.m_Value) == 0;
#else
            return atomic.m_Value == 0;
#endif // SUPPORTS_THREADING
        }

        #endregion // Write

        public sealed class DeadlockException : Exception {
            public DeadlockException(string message) : base(message) { }
        }
    }
}