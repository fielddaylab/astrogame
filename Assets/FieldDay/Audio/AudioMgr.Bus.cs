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
using FieldDay.Filters;
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

            public UniqueId16 Handle;
            public FloatTweenIndices FloatTweens;
            public StringHash32 Name;

            public StringHash32 DuckingMix;
            public SignalEnvelope DuckingEnvelope;
        }

        #endregion // Bus Data

        #region Bus Creation

        private void CreateBus(StringHash32 id, AudioPropertyBlock busProperties, StringHash32 parentId, StringHash32 duckingMix, in SignalEnvelope duckingEnvelope) {
            if (m_BusCount >= MaxBuses) {
                throw new InvalidOperationException("Maximum number of audio buses created");
            }

            int idx = m_BusCount++;
            m_BusNameToIndex.Add(id.HashValue, idx);
            m_BusData[idx].Name = id;
            m_BusData[idx].BusProperties = busProperties;
            m_BusData[idx].DuckingMix = duckingMix;
            m_BusData[idx].DuckingEnvelope = duckingEnvelope;

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
                return -1;
            }

            return index;
        }

        #endregion // Lookup

        #region Bindings

        private unsafe void ProcessLateBindings() {
            // create buses
            if (m_BusLateBindQueue.Count > 0) {
                ProcessBusDependencies();
            }

            while (m_EventLateBindQueue.TryPopFront(out AudioEvent evt)) {
                evt.CachedBusIndex = FindBusIndexForId(evt.Bus);
            }

            while(m_MixStateLateBindQueue.TryPopFront(out AudioMixState mix)) {
                GenerateMixStateData(mix);
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
                CreateBus(bus.AssetId, bus.Properties, bus.ParentId, bus.DuckingMix, bus.DuckingEnvelope);
            }

            m_BusLateBindQueue.Clear();
        }

        #endregion // Bindings

#if UNITY_EDITOR

        static internal void ReloadAudioMixState(AudioMixState mix) {
            if (!mix.Linked) {
                return;
            }

            AudioMgr mgr = Game.Audio;
            mgr.GenerateMixStateData(mix);

            for(int i = mgr.m_ActiveMixStates.Count; i-- > 0;) {
                ref MixData mixData = ref mgr.m_ActiveMixStates[i];
                if (mixData.Id == mix.CachedId) {
                    mixData.Block = mix.MixBlock;
                }
            }
        }

#endif // UNITY_EDITOR

        private unsafe void UpdateBuses() {
            AudioPropertyBlock block = default;
            for (int i = 0; i < m_BusCount; i++) {
                ref BusData bus = ref m_BusData[i];
                AudioPropertyBlock.Combine(AudioPropertyBlock.Default, bus.BusProperties, ref block);
                AudioPropertyBlock.Combine(block, *bus.ScriptProperties, ref block);
#if DEVELOPMENT
                AudioPropertyBlock.Combine(block, m_DebugBusProperties[i], ref block);
#endif // DEVELOPMENT
                block.Volume *= bus.ConfigVolume;
                m_WorkingBusProperties[i] = block;
            }
        }

        private void PropagateBusProperties() {
            for (int i = 1; i < m_BusCount; i++) {
                ref BusData bus = ref m_BusData[i];
                AudioPropertyBlock.Combine(m_WorkingBusProperties[i], m_WorkingBusProperties[bus.ParentIndex], ref m_WorkingBusProperties[i]);
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
                    ProcessDucking(ref bus, isPlaying);
                    busIndex = bus.ParentIndex;
                }
            }
        }

        private void ProcessDucking(ref BusData bus, bool isPlaying) {
            if (bus.DuckingMix.IsEmpty || bus.InstanceCount != (isPlaying ? 1 : 0)) {
                return;
            }

            if (isPlaying) {
                SetMixStateTarget(bus.DuckingMix, 1, bus.DuckingEnvelope.Attack, false, false);
            } else {
                SetMixStateTarget(bus.DuckingMix, 0, bus.DuckingEnvelope.Decay, false, false);
            }
        }
    }
}