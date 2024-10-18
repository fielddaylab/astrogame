#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

#if DEVELOPMENT
    #define MEMORY_LEAK_DETECTION
#endif // DEVELOPMENT

using System;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using UnityEngine;

namespace FieldDay.Memory {

    /// <summary>
    /// Manages memory pools.
    /// </summary>
    public class MemoryMgr {
        private int m_LastKnownGenerationCount;
        private int[] m_GCCollectCounts;
        private long[] m_GCCollectTimestamps;
        private long m_MostRecentGCTimestamp;

        private long m_MainThreadAllocationTracker;

        private IPool<Mesh> m_MeshPool;
        private IPool<Material> m_MaterialPool;
        private Shader m_DefaultShader;

        private Unsafe.ArenaHandle m_BudgetCategoryAllocator;

#if MEMORY_LEAK_DETECTION
        private RingBuffer<Unsafe.ArenaHandle> m_ArenaTracker;
#endif // MEMORY_LEAK_DETECTION

        #region Mesh

        #endregion // Mesh

        #region GC

        internal void UpdateGCMarkers(ushort frameIndex) {
            long now = Stopwatch.GetTimestamp();

            int maxGen = GC.MaxGeneration + 1;
            int genMask = 0;

            if (m_LastKnownGenerationCount < maxGen) {
                m_LastKnownGenerationCount = maxGen;
                Array.Resize(ref m_GCCollectCounts, maxGen);
                Array.Resize(ref m_GCCollectTimestamps, maxGen);
                Log.Msg("[MemoryMgr] Generation count changed to {0}", maxGen);
            }

            for (int i = 0; i < maxGen; i++) {
                int genCount = GC.CollectionCount(i);
                if (Ref.Replace(ref m_GCCollectCounts[i], genCount)) {
                    m_GCCollectTimestamps[i] = now;
                    genMask |= (1 << i);
                }
            }

            if (genMask != 0) {
                if (DebugFlags.IsFlagSet(DebuggingFlags.LogGCState)) {
                    Log.Trace("[MemoryMgr] Garbage collected {0}", genMask);
                }
                m_MostRecentGCTimestamp = now;
                Mem.InvokeGCOccurred(genMask);
            }

            long allocated = GC.GetTotalMemory(false);
            if (m_MainThreadAllocationTracker != allocated) {
                if (DebugFlags.IsFlagSet(DebuggingFlags.LogGCState)) {
                    long diff = allocated - m_MainThreadAllocationTracker;
                    Log.Trace("[MemoryMgr] GC allocated {0}b", diff);
                }
                m_MainThreadAllocationTracker = allocated;
            }
        }

        #endregion // GC

        #region Arenas

        /// <summary>
        /// Creates a new memory arena.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Unsafe.ArenaHandle CreateArena(int length, StringHash32 name, Unsafe.AllocatorFlags flags) {
            Unsafe.ArenaHandle handle = Unsafe.CreateArena(length, name, flags);
#if MEMORY_LEAK_DETECTION
            m_ArenaTracker.PushBack(handle);
#endif // MEMORY_LEAK_DETECTION
            return handle;
        }

        /// <summary>
        /// Destroys a memory arena.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyArena(Unsafe.ArenaHandle arena) {
#if MEMORY_LEAK_DETECTION
            if (!m_ArenaTracker.FastRemove(arena)) {
                Assert.Fail("Arena was not created from MemoryMgr");
            }
#endif // MEMORY_LEAK_DETECTION
            Unsafe.DestroyArena(arena);
        }

        /// <summary>
        /// Destroys a memory arena.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DestroyArena(ref Unsafe.ArenaHandle arena) {
#if MEMORY_LEAK_DETECTION
            if (!m_ArenaTracker.FastRemove(arena)) {
                Assert.Fail("Arena was not created from MemoryMgr");
            }
#endif // MEMORY_LEAK_DETECTION
            Unsafe.TryDestroyArena(ref arena);
        }

        #endregion // Arenas

        #region Budget

        #endregion // Budget

        #region Events

        internal MemoryMgr() {
            Mem.Mgr = this;
        }

        internal void Initialize(MemoryPoolConfiguration configuration) {
            int genCount = GC.MaxGeneration + 1;
            m_GCCollectCounts = new int[genCount];
            m_GCCollectTimestamps = new long[genCount];
            m_LastKnownGenerationCount = genCount;

            m_MeshPool = new DynamicPool<Mesh>(configuration.MeshCapacity, (p) => new Mesh(), false);
            m_MeshPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

            m_MaterialPool = new DynamicPool<Material>(configuration.MaterialCapacity, (p) => new Material(m_DefaultShader), false);
            m_MaterialPool.Config.RegisterOnDestruct((p, m) => GameObject.DestroyImmediate(m));

            Log.Msg("[MemoryMgr] GC Latency is {0}, {1} generations", GCSettings.LatencyMode, genCount);
            //GCSettings.LatencyMode = GCLatencyMode.LowLatency;

            m_DefaultShader = Shader.Find("Hidden/InternalColored");

#if MEMORY_LEAK_DETECTION
            m_ArenaTracker = new RingBuffer<Unsafe.ArenaHandle>(64, RingBufferMode.Expand);
#endif // MEMORY_LEAK_DETECTION
        }

        internal void Shutdown() {
            m_MeshPool.Dispose();
            m_MaterialPool.Dispose();
            m_DefaultShader = null;

#if MEMORY_LEAK_DETECTION
            if (m_ArenaTracker.Count > 0) {
                Log.Error("[MemoryMgr] MEMORY LEAK - {0} arenas", m_ArenaTracker);
                while (m_ArenaTracker.TryPopBack(out var arena)) {
                    Log.Error("LEAKED {0}", arena.ToDebugString());
                    Unsafe.DestroyArena(arena);
                }
            }
#endif // MEMORY_LEAK_DETECTION

            Mem.Mgr = null;
        }

        #endregion // Events

        #region Debugging

        private enum DebuggingFlags {
            LogGCState
        }

#if DEVELOPMENT

        [EngineMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo info = new DMInfo("MemoryMgr");

            DebugFlags.Menu.AddFlagToggle(info, "Log All GC Events", DebuggingFlags.LogGCState);

            return info;
        }

#endif // DEVELOPMENT
        
        #endregion // Debugging
    }

    [Serializable]
    public struct MemoryPoolConfiguration {
        public int MeshCapacity;
        public int MaterialCapacity;
        public int UnmanagedBudgetMB;
    }
}