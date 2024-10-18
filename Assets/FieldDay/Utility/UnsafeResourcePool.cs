using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay {
    public unsafe struct UnsafeResourcePool<T> where T : unmanaged {
        private UnsafeBitSet m_BitMap;
        private UnsafeSpan<T> m_Data;
        private int m_AllocHead;

        public void Create(Unsafe.ArenaHandle arena, int amount) {
            m_BitMap = new UnsafeBitSet(arena.AllocSpan<uint>(Unsafe.AlignUp32(amount) / 32));
            m_BitMap.Clear();

            m_Data = arena.AllocSpan<T>(amount);
        }

        /// <summary>
        /// Allocates a new instance from the pool.
        /// </summary>
        public T* Alloc() {
            Assert.True(m_BitMap.Count < m_Data.Length, "Pool is full");
            while(m_BitMap.IsSet(m_AllocHead)) {
                m_AllocHead = (m_AllocHead + 1) % m_Data.Length;
            }
            T* ptr = m_Data.Ptr + m_AllocHead;
            m_BitMap.Set(m_AllocHead);
            m_AllocHead = (m_AllocHead + 1) % m_Data.Length;
            *ptr = default(T);
            return ptr;
        }

        /// <summary>
        /// Frees the given instance back to the pool.
        /// </summary>
        public void Free(T* ptr) {
            int chunkIdx = (int) (ptr - m_Data.Ptr);
            Assert.True(chunkIdx >= 0 && chunkIdx < m_Data.Length);
            m_BitMap.Unset(chunkIdx);
        }

        /// <summary>
        /// Frees the given instance back to the pool.
        /// </summary>
        public void TryFree(ref T* ptr) {
            if (ptr != null) {
                Free(ptr);
                ptr = null;
            }
        }
    }
}