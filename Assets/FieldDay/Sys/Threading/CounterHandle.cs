using System;
using System.Runtime.CompilerServices;
using System.Threading;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Threading {
    /// <summary>
    /// Work counter.
    /// </summary>
    public struct CounterHandle : IDisposable {
        internal unsafe int* Value;

        /// <summary>
        /// Returns if this counter is a valid counter.
        /// </summary>
        public bool IsValid() {
            unsafe {
                return Value != null && *Value >= 0;
            }
        }

        /// <summary>
        /// Returns the value of this counter.
        /// </summary>
        public int GetValue() {
            unsafe {
                return Value == null ? -1 : *Value;
            }
        }

        /// <summary>
        /// Resets the counter.
        /// </summary>
        public int Reset() {
            unsafe {
                if (Value == null) {
                    return -1;
                } else {
                    int val = *Value;
                    *Value = 0;
                    return val;
                }
            }
        }

        /// <summary>
        /// Returns if the counter is done.
        /// </summary>
        public bool IsDone() {
            unsafe {
                return Value == null || *Value == 0;
            }
        }

        /// <summary>
        /// Decrements the counter.
        /// </summary>
        public void Decrement() {
            unsafe {
                if (Value != null) {
                    Assert.True(*Value > 0);
                    Interlocked.Decrement(ref *Value);
                }
            }
        }

        /// <summary>
        /// Increments the counter.
        /// </summary>
        public void Increment() {
            unsafe {
                if (Value != null) {
                    Interlocked.Increment(ref *Value);
                }
            }
        }

        public void Dispose() {
            unsafe {
                s_Pool.Free(ref Value);
            }
        }

        #region Pool

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public CounterHandle Alloc() {
            return Alloc(0);
        }

        static public CounterHandle Alloc(int count) {
            unsafe {
                int* data = s_Pool.Alloc();
                *data = count;
                CounterHandle handle;
                handle.Value = data;
                return handle;
            }
        }

        static private Pool s_Pool;

        private unsafe struct Pool {
            private readonly UnsafeSpan<int> m_Memory;
            private int m_InUse;
            private int m_Head;

            public Pool(int capacity) {
                m_Memory = Unsafe.AllocSpan<int>(capacity);
                m_InUse = 0;
                for(int i = 0; i < m_Memory.Length; i++) {
                    m_Memory[i] = -1;
                }
                m_Head = 0;
            }

            public void Dispose() {
                void* ptr = m_Memory.Ptr;
                Unsafe.TryFree(ref ptr);
            }

            public int* Alloc() {
                Assert.True(m_InUse < m_Memory.Length);
                while (m_Memory[m_Head] >= 0) {
                    m_Head = (m_Head + 1) % m_Memory.Length;
                }
                unsafe {
                    int* ptr = m_Memory.Ptr + m_Head;
                    m_InUse++;
                    return ptr;
                }
            }

            public void Free(ref int* ptr) {
                if (ptr != null && *ptr >= 0) {
                    Assert.True(ptr >= m_Memory.Ptr && ptr < m_Memory.Ptr + m_Memory.Length);
                    *ptr = -1;
                    m_InUse--;
                    ptr = null;
                }
            }
        }

        static internal void InitializeAllocator(int capacity) {
            s_Pool.Dispose();
            s_Pool = new Pool(capacity);
        }

        static internal void DestroyAllocator() {
            s_Pool.Dispose();
        }

        #endregion // Pool
    }
}