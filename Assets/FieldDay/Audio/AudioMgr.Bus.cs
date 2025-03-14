#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using System.Runtime.CompilerServices;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Mathematics;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Bus Data

        private unsafe struct BusData {
            public AudioPropertyBlock BusProperties;
            public AudioPropertyBlock* ScriptProperties;
            public int ParentIndex;
            public float ConfigVolume;

            public int InstanceCount;
            public AudioPropertyBlock LastKnownProperties;

            public UniqueId16 Handle;
            public FloatTweenIndices FloatTweens;
            public StringHash32 Name;
        }

        #endregion // Bus Data

        #region Bus Creation

        private void CreateBus(StringHash32 id, AudioPropertyBlock busProperties, StringHash32 parentId) {
            if (m_BusCount >= MaxBuses) {
                throw new InvalidOperationException("Maximum number of audio buses created");
            }

            int idx = m_BusCount++;
            m_BusNameToIndex.Add(id.HashValue, idx);
            m_BusData[idx].Name = id;
            m_BusData[idx].BusProperties = busProperties;

            if (!parentId.IsEmpty) {
                m_BusData[idx].ParentIndex = FindBusIndexForId(parentId);
            } else {
                m_BusData[idx].ParentIndex = idx == 0 ? -1 : 0;
            }

            Log.Msg("[AudioMgr] Created bus '{0}'", id.ToDebugString());
        }

        #endregion // Bus Creation

        #region Lookup

        private ref BusData FindBusForId(StringHash32 id) {
            if (!m_BusNameToIndex.TryGetValue(id.HashValue, out int index)) {
                Log.Error("[AudioMgr] No bus with id '{0}'", id.ToDebugString());
                return ref Unsafe.NullRef<BusData>();
            }

            return ref m_BusData[index];
        }

        private int FindBusIndexForId(StringHash32 id) {
            if (!m_BusNameToIndex.TryGetValue(id.HashValue, out int index)) {
                Log.Error("[AudioMgr] No bus with id '{0}'", id.ToDebugString());
                return 0;
            }

            return index;
        }

        #endregion // Lookup

        #region Bindings

        private void ProcessLateBindings() {
            // create buses
            if (m_BusLateBindQueue.Count > 0) {
                ProcessBusDependencies();
            }

            while (m_EventLateBindQueue.TryPopFront(out AudioEvent evt)) {
                evt.CachedBusIndex = FindBusIndexForId(evt.Bus);
            }
        }

        private unsafe void ProcessBusDependencies() {
            int busCount = m_BusLateBindQueue.Count;
            DependencySolver.Node<StringHash32>* nodes = stackalloc DependencySolver.Node<StringHash32>[busCount];
            DependencySolver.Edge<StringHash32>* edges = stackalloc DependencySolver.Edge<StringHash32>[busCount];
            
            for(int i = 0; i < busCount; i++) {
                AudioBus bus = m_BusLateBindQueue[i];
                nodes[i].Id = bus.AssetId;
                if (!bus.ParentId.IsEmpty) {
                    edges[i].Endpoint = bus.ParentId;
                    nodes[i].Edges = new OffsetLengthU16((ushort) i, 1);
                } else {
                    nodes[i].Edges = default;
                }
            }

            DependencySolver.OutputNode<StringHash32>* outputNodes = stackalloc DependencySolver.OutputNode<StringHash32>[busCount];
            DependencySolver.Result result = DependencySolver.Solve<StringHash32>(new UnsafeSpan<DependencySolver.Node<StringHash32>>(nodes, busCount), new UnsafeSpan<DependencySolver.Edge<StringHash32>>(edges, busCount), new UnsafeSpan<DependencySolver.OutputNode<StringHash32>>(outputNodes, busCount));
            Assert.True(result == DependencySolver.Result.Success);

            for(int i = 0; i < busCount; i++) {
                var output = outputNodes[i];
                AudioBus bus = m_BusLateBindQueue[output.OriginalIndex];
                CreateBus(bus.AssetId, bus.Properties, bus.ParentId);
            }

            m_BusLateBindQueue.Clear();
        }

        #endregion // Bindings

        private unsafe void UpdateBuses() {
            AudioPropertyBlock block;
            for(int i = 0; i < m_BusCount; i++) {
                ref BusData bus = ref m_BusData[i];
                block = bus.ParentIndex < 0 ? AudioPropertyBlock.Default : m_BusData[bus.ParentIndex].LastKnownProperties;
                AudioPropertyBlock.Combine(block, bus.BusProperties, ref block);
                AudioPropertyBlock.Combine(block, *bus.ScriptProperties, ref block);
#if DEVELOPMENT
                AudioPropertyBlock.Combine(block, m_DebugBusProperties[i], ref block);
#endif // DEVELOPMENT
                block.Volume *= bus.ConfigVolume;
                bus.LastKnownProperties = block;
            }
        }

        private void UpdatePlayingInstanceCount(UniqueId16 id, int busIndex, bool isPlaying) {
            Assert.True(m_VoiceIdAllocator.IsValid(id));
            if (isPlaying != m_VoicePlayingBitmap.IsSet(id.Index)) {
                m_VoicePlayingBitmap.Set(id.Index, isPlaying);

                int increment = isPlaying ? 1 : -1;
                while(busIndex >= 0) {
                    ref BusData bus = ref m_BusData[busIndex];
                    bus.InstanceCount += increment;
                    busIndex = bus.ParentIndex;
                }
            }
        }
    }
}