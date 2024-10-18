using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FieldDay.Threading {
    static public unsafe class UnityAtomics {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        static public bool AreEqual(AtomicSafetyHandle a, AtomicSafetyHandle b) {
            return SafetyHandleWrapper.Wrap(a).Equals(SafetyHandleWrapper.Wrap(b));
        }

        private struct SafetyHandleWrapper : IEquatable<SafetyHandleWrapper> {
            private IntPtr versionNode;
            private int version;
            private int staticSafetyId;

            public bool Equals(SafetyHandleWrapper other) {
                return versionNode == other.versionNode
                    && version == other.version
                    && staticSafetyId == other.staticSafetyId;
            }

            static public SafetyHandleWrapper Wrap(AtomicSafetyHandle handle) {
                return *(SafetyHandleWrapper*) &handle;
            }
        }
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
    }

    /// <summary>
    /// Helper class for converting unsafe data to native arrays
    /// for use within a job.
    /// </summary>
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    public sealed unsafe class UnityJobAtomicsHelper {
#else
    public unsafe struct UnityJobAtomicsHelper {
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private readonly AtomicSafetyHandle[] m_Handles;
        private int m_AllocatedHandles;
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS

        public UnityJobAtomicsHelper(int capacity) {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Handles = new AtomicSafetyHandle[capacity];
            m_AllocatedHandles = 0;
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeArray<T> Convert<T>(UnsafeSpan<T> data) where T : unmanaged {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArray<T> nativeArray = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.Ptr, data.Length, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AllocHandle());
            return nativeArray;
#else
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(data.Ptr, data.Length, Allocator.None);
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void Release<T>(NativeArray<T> data) where T : unmanaged {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var handle = NativeArrayUnsafeUtility.GetAtomicSafetyHandle(data);
            FreeHandle(handle);
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void Reset() {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            while (m_AllocatedHandles > 0) {
                AtomicSafetyHandle.Release(m_Handles[--m_AllocatedHandles]);
            }
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle AllocHandle() {
            if (m_AllocatedHandles >= m_Handles.Length) {
                throw new InsufficientMemoryException(string.Format("Ran out of AtomicSafetyHandles with pool size {0}", m_Handles.Length));
            }

            AtomicSafetyHandle handle = AtomicSafetyHandle.Create();
            m_Handles[m_AllocatedHandles++] = handle;
            return handle;
        }

        private void FreeHandle(AtomicSafetyHandle handle) {
            if (m_AllocatedHandles >= m_Handles.Length) {
                throw new InsufficientMemoryException(string.Format("Ran out of AtomicSafetyHandles with pool size {0}", m_Handles.Length));
            }
            Assert.True(AtomicSafetyHandle.IsHandleValid(handle), "Invalid handle");
            
            for(int i = 0; i < m_AllocatedHandles; i++) {
                if (UnityAtomics.AreEqual(m_Handles[i], handle)) {
                    AtomicSafetyHandle.Release(handle);
                    ArrayUtils.FastRemoveAt(m_Handles, ref m_AllocatedHandles, i);
                    break;
                }
            }

            throw new InvalidOperationException("Handle was not allocated with this arena");
        }
#endif // ENABLE_UNITY_COLLECTIONS_CHECKS
    }
}