using System.Runtime.CompilerServices;
using BeauUtil;

namespace FieldDay.Pipes {
    public interface IPipe<T> where T : struct {
        /// <summary>
        /// Returns if the pipe is currently full.
        /// </summary>
        bool IsFull();

        /// <summary>
        /// Writes a value to the pipe.
        /// </summary>
        void Write(T value);

        /// <summary>
        /// Writes a set of value to the pipe.
        /// </summary>
        void Write(T[] values, int offset, int length);

        /// <summary>
        /// Reads a value from the pipe.
        /// </summary>
        T Read();

        /// <summary>
        /// Attempts to read a value from the pipe.
        /// </summary>
        bool TryRead(out T value);

        /// <summary>
        /// Attempts to peek at the next value in the pipe.
        /// </summary>
        bool TryPeek(out T value);

        /// <summary>
        /// Clears all values from the pipe.
        /// </summary>
        void Clear();
    }

    //public struct UnsafePipe<T> : IPipe<T> where T : unmanaged {
    //    private UnsafeSpan<T> m_Data;

    //    public UnsafeSpan<T> GetBuffer() {
    //        return m_Data;
    //    }
    //}

    /// <summary>
    /// Data pipe.
    /// </summary>
    public struct Pipe<T> : IPipe<T> where T : struct {
        private RingBuffer<T> m_Data;

        public Pipe(int capacity, bool flexible) {
            m_Data = new RingBuffer<T>(capacity, flexible ? RingBufferMode.Expand : RingBufferMode.Fixed);
        }

        public Pipe(RingBuffer<T> data) {
            m_Data = data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFull() {
            return m_Data.IsFull();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(T value) {
            m_Data.PushBack(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(T[] values, int offset, int length) {
            for(int i = offset, end = offset + length; i < end; i++) {
                Write(values[i]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read() {
            return m_Data.PopFront();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryRead(out T value) {
            return m_Data.TryPopFront(out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryPeek(out T value) {
            return m_Data.TryPeekFront(out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear() {
            m_Data.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RingBuffer<T> GetBuffer() {
            return m_Data;
        }
    }
}