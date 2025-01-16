using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Rendering;

namespace FieldDay.Rendering {
    /// <summary>
    /// Instanced mesh rendering data buffer.
    /// Will automatically flush once full.
    /// </summary>
    public unsafe struct InstancedMeshBuffer<T> : IDisposable where T : unmanaged {
        private T* m_BufferHead;
        private T* m_WriteHead;
        private ushort m_QueuedElements;
        private readonly ushort m_MaxElements;

        private RenderParams m_RenderParams;
        private Mesh m_Mesh;
        private readonly int m_SubmeshIndex;

        public InstancedMeshBuffer(T* buffer, int bufferSize, RenderParams renderParams, Mesh mesh, int submeshIndex = 0) {
            m_BufferHead = buffer;
            m_MaxElements = (ushort) Math.Max(4, Math.Min(bufferSize, 1023));
            m_WriteHead = buffer;
            m_QueuedElements = 0;
            m_RenderParams = renderParams;
            m_Mesh = mesh;
            m_SubmeshIndex = submeshIndex;
        }

        public void Dispose() {
            m_BufferHead = m_WriteHead = null;
            m_QueuedElements = 0;
            m_RenderParams = default;
            m_Mesh = null;
        }
        
        /// <summary>
        /// Returns if this buffer has hit its limit.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFull() {
            return m_QueuedElements == m_MaxElements;
        }

        /// <summary>
        /// Submits all queued instances to be rendered.
        /// </summary>
        public void Flush() {
            if (m_QueuedElements > 0) {
                Graphics.RenderMeshInstanced<T>(m_RenderParams, m_Mesh, m_SubmeshIndex, Unsafe.NativeArray(m_BufferHead, m_QueuedElements), m_QueuedElements);
                m_QueuedElements = 0;
                m_WriteHead = m_BufferHead;
            }
        }

        /// <summary>
        /// Queues an instanced mesh.
        /// </summary>
        public void Queue(T data) {
            if (IsFull()) {
                Flush();
            }

            Assert.True(m_QueuedElements < m_MaxElements);
            *m_WriteHead++ = data;
            m_QueuedElements++;
        }

        /// <summary>
        /// Queues an instanced mesh.
        /// </summary>
        public void Queue(ref T data) {
            if (IsFull()) {
                Flush();
            }

            Assert.True(m_QueuedElements < m_MaxElements);
            *m_WriteHead++ = data;
            m_QueuedElements++;
        }

        /// <summary>
        /// Queues an instanced mesh.
        /// </summary>
        public void Queue(T* data) {
            if (IsFull()) {
                Flush();
            }

            Assert.True(data != null, "Cannot queue null instanced mesh parameters");
            Assert.True(m_QueuedElements < m_MaxElements);
            *m_WriteHead++ = *data;
            m_QueuedElements++;
        }
    }

    public struct DefaultInstancedMeshParams {
        public Matrix4x4 objectToWorld;
    }
}