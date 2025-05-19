#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using System;
using System.Collections.Generic;
using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Pipes;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Types

        [Serializable]
        public struct Config {
            public bool Is3D;
            public AudioEmitterProfile DefaultEmitterProfile;
            public float PreloadWorkerTimeSlice;
        }

        #endregion // Types

        public const int MaxVoices = 80;
        public const int MaxBuses = 16;

        #region State

        private GameObject m_AudioSourceRoot;
        private AudioEmitterConfig m_DefaultEmitterConfig;
        private bool m_HasSpatializationPlugin;
        private float m_PreloadWorkerTimeSlice;

        private Pipe<AudioCommand> m_CommandPipe = new Pipe<AudioCommand>(128, true);
        private Pipe<PlayCommandData> m_PlayCommandPipe = new Pipe<PlayCommandData>(64, true);
        private UniqueIdAllocator16 m_VoiceIdAllocator = new UniqueIdAllocator16(MaxVoices + MaxBuses);

        private BitSet128 m_VoicePlayingBitmap;

        private Unsafe.ArenaHandle m_Arena;
        private UnsafeResourcePool<AudioPropertyBlock> m_TargetablePropertyBlocks;

        private LLTable<PositionSyncData> m_PositionSyncTable;
        private LLIndexList m_PositionSyncList;

        private LLTable<FloatParamTweenData> m_FloatTweenTable;
        private LLIndexList m_FloatTweenList;

        private IPool<AudioVoiceComponents> m_VoiceComponentPool;
        private IPool<VoiceData> m_VoiceDataPool;

        private BusData[] m_BusData;
        private int m_BusCount;

        private AudioListener m_ListenerReference;

#if DEVELOPMENT
        private AudioPropertyBlock[] m_DebugBusProperties;
#endif // DEVELOPMENT

        private RingBuffer<VoiceData> m_ActiveVoices = new RingBuffer<VoiceData>(MaxVoices);
        private RingBuffer<AudioClip> m_PreloadQueue = new RingBuffer<AudioClip>(32, RingBufferMode.Expand);
        private RingBuffer<AudioEvent> m_EventLateBindQueue = new RingBuffer<AudioEvent>(128, RingBufferMode.Expand);
        private RingBuffer<AudioBus> m_BusLateBindQueue = new RingBuffer<AudioBus>(MaxBuses);

        private readonly Dictionary<uint, int> m_BusNameToIndex = new Dictionary<uint, int>(MaxBuses);

        #endregion // State

        internal unsafe AudioMgr(Config config) {
            m_Arena = Unsafe.CreateArena(1 * Unsafe.MiB, "Audio", Unsafe.AllocatorFlags.ZeroOnAllocate);
            m_TargetablePropertyBlocks.Create(m_Arena, (MaxVoices + MaxBuses) * 2);
            m_PreloadWorkerTimeSlice = config.PreloadWorkerTimeSlice;

            m_PositionSyncTable = new LLTable<PositionSyncData>(MaxVoices);
            m_FloatTweenTable = new LLTable<FloatParamTweenData>((MaxVoices + MaxBuses) * 2);
            m_PositionSyncList = m_FloatTweenList = LLIndexList.Empty;

            m_VoiceDataPool = new FixedPool<VoiceData>(MaxVoices, Pool.DefaultConstructor<VoiceData>());
            m_VoiceDataPool.Prewarm(MaxVoices);

            m_BusData = new BusData[MaxBuses];
            for(int i = 0; i < MaxBuses; i++) {
                ref BusData bus = ref m_BusData[i];
                bus.ScriptProperties = m_TargetablePropertyBlocks.Alloc();

                *bus.ScriptProperties = AudioPropertyBlock.Default;
                bus.LastKnownProperties = AudioPropertyBlock.Default;

                bus.ConfigVolume = 1;

                bus.Handle = m_VoiceIdAllocator.Alloc();
                bus.FloatTweens.Reset();
            }

#if DEVELOPMENT
            m_DebugBusProperties = new AudioPropertyBlock[MaxBuses];
            for(int i = 0; i < MaxBuses; i++) {
                m_DebugBusProperties[i] = AudioPropertyBlock.Default;
            }
#endif // DEVELOPMENT

            m_AudioSourceRoot = new GameObject("AudioMgr");
            m_AudioSourceRoot.hideFlags |= HideFlags.NotEditable | HideFlags.DontSave;
            GameObject.DontDestroyOnLoad(m_AudioSourceRoot);

            m_VoiceComponentPool = new FixedPool<AudioVoiceComponents>(MaxVoices, ConstructNewSource);
            m_VoiceComponentPool.Config.RegisterOnDestruct((p, a) => GameObject.Destroy(a.gameObject));
            m_VoiceComponentPool.Config.RegisterOnFree((p, a) => {
                a.Source.Stop();
                a.Source.clip = null;
#if UNITY_EDITOR
                a.gameObject.name = "unused audio voice";
#endif // UNITY_EDITOR
                a.enabled = false;
                a.PlayingHandle = default;
            });
            m_VoiceComponentPool.Prewarm(MaxVoices);

            if (config.DefaultEmitterProfile) {
                m_DefaultEmitterConfig = config.DefaultEmitterProfile.Config;
                if (!Game.Assets.HasNamed<AudioEmitterProfile>(config.DefaultEmitterProfile.AssetId)) {
                    Game.Assets.AddNamed(config.DefaultEmitterProfile.AssetId, config.DefaultEmitterProfile);
                }
            } else {
                m_DefaultEmitterConfig = config.Is3D ? AudioEmitterConfig.Default3D : AudioEmitterConfig.Default2D;
            }

            m_HasSpatializationPlugin = !string.IsNullOrEmpty(AudioSettings.GetSpatializerPluginName());

            Game.Assets.SetNamedAssetLoadCallbacks<AudioEvent>(OnAudioEventLoaded, OnAudioEventUnloaded);
            Game.Assets.SetNamedAssetLoadCallbacks<AudioBus>(OnAudioBusLoaded, OnAudioBusUnloaded);

            m_BusNameToIndex.Add(0, 0);
            CreateBus(AudioBus.Master, AudioPropertyBlock.Default, default);
        }

        #region Events

        internal void PreUpdate(float deltaTime) {
            using (Profiling.Sample("AudioMgr::PreUpdate")) {
                ProcessLateBindings();
                CullFinishedVoices();
                FlushCommandPipe();
            }
        }

        internal void Update(float deltaTime) {
            using (Profiling.Sample("AudioMgr::Update")) {
                ProcessLateBindings();

                FlushCommandPipe();

                if (m_PreloadQueue.Count > 0) {
                    WorkSlicer.TimeSliced(m_PreloadQueue, HandlePreloadDelegate, m_PreloadWorkerTimeSlice / 2);
                }
            }
        }

        internal void LateUpdate(float deltaTime) {
            using (Profiling.Sample("AudioMgr::LateUpdate")) {
                ProcessLateBindings();

                FlushCommandPipe();

                if (m_PreloadQueue.Count > 0) {
                    WorkSlicer.TimeSliced(m_PreloadQueue, HandlePreloadDelegate, m_PreloadWorkerTimeSlice / 2);
                }

                SyncEmitterLocations();
                UpdateTweens(deltaTime);
                UpdateBuses();
                UpdateVoices(deltaTime, Time.realtimeSinceStartupAsDouble);

                switch (Frame.Index % 60) {
                    case 0: {
                        m_FloatTweenTable.Linearize(ref m_FloatTweenList);
                        break;
                    }
                    case 1: {
                        m_FloatTweenTable.OptimizeFreeList();
                        break;
                    }
                    case 2: {
                        m_PositionSyncTable.Linearize(ref m_PositionSyncList);
                        break;
                    }
                    case 3: {
                        m_PositionSyncTable.OptimizeFreeList();
                        break;
                    }
                }

#if DEVELOPMENT
                if (DebugFlags.IsFlagSet(DebuggingFlags.DisplayStats)) {
                    using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                        psb.Builder.Append("Voice Count: ").AppendNoAlloc(m_ActiveVoices.Count)
                            .Append("\n   Active Tweens: ").AppendNoAlloc(m_FloatTweenList.Length)
                            .Append("\n   Active Position Trackers: ").AppendNoAlloc(m_PositionSyncList.Length)
                            .Append("\n   Clip Preload Queue: ").AppendNoAlloc(m_PreloadQueue.Count);

                        DebugDraw.AddLogText(psb, ColorBank.Aqua);
                    }
                }

                if (DebugFlags.IsFlagSet(DebuggingFlags.DisplayVoiceList)) {
                    using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                        psb.Builder.Append("Voice Count: ").AppendNoAlloc(m_ActiveVoices.Count);
                        foreach(var voice in m_ActiveVoices) {
                            psb.Builder.Append("\n   ").Append(voice.DebugName);
                            AudioSource src = voice.Components.Source;
                            psb.Builder.Append(" (").AppendNoAlloc(src.time, 2).Append('/')
                                .AppendNoAlloc(src.clip.length, 2);
                            if (src.loop) {
                                psb.Builder.Append('L');
                            }
                            psb.Builder.Append(") ");
                            switch(voice.State) {
                                case VoiceState.Paused: {
                                    psb.Builder.Append("[PAUSED]");
                                    break;
                                }
                                case VoiceState.PlayRequested: {
                                    psb.Builder.Append("[QUEUED]");
                                    break;
                                }
                                case VoiceState.Stopped: {
                                    psb.Builder.Append("[DONE]");
                                    break;
                                }
                            }
                        }

                        DebugDraw.AddViewportText(new Vector2(0, 1), new Vector2(8, -8), psb, ColorBank.Teal, 0, TextAnchor.UpperLeft, DebugTextStyle.BackgroundDark); ;
                    }
                }
#endif // DEVELOPMENT
            }
        }

        internal void Shutdown() {
            Unsafe.TryDestroyArena(ref m_Arena);
            m_TargetablePropertyBlocks = default;

            foreach(var voice in m_ActiveVoices) {
                if ((voice.Flags & AudioPlaybackFlags.UseProvidedSource) == 0) {
                    m_VoiceComponentPool.Free(voice.Components);
                }
            }
            m_VoiceComponentPool.Clear();
        }

        #endregion // Events

        #region Asset Handlers

        static private WorkSlicer.ElementOperation<AudioClip> HandlePreloadDelegate = HandlePreload;

        static private void HandlePreload(AudioClip clip) {
            if (clip.loadState == AudioDataLoadState.Unloaded) {
                using (Profiling.Time("AudioMgr.HandlePreload", ProfileTimeUnits.Microseconds)) {
                    clip.LoadAudioData();
                }
                Log.Debug("[AudioMgr] Preloaded clip '{0}'", clip.name);
            }
        }

        private void OnAudioEventLoaded(AudioEvent evt) {
            if (evt.PreloadSamples) {
                foreach (var clip in evt.Samples) {
                    if (!clip.preloadAudioData && clip.loadState == AudioDataLoadState.Unloaded) {
                        m_PreloadQueue.PushBack(clip);
                    }
                }
            }

            if (evt.CachedBusIndex < 0) {
                m_EventLateBindQueue.PushBack(evt);
            }
        }

        private void OnAudioEventUnloaded(AudioEvent evt) {
            // nothing
        }

        private void OnAudioBusLoaded(AudioBus bus) {
            if (m_BusNameToIndex.ContainsKey(bus.AssetId.HashValue)) {
                Log.Error("[AudioMgr] Bus '{0}' already loaded!", bus.AssetId);
                return;
            }

            if (m_BusCount > 1) {
                Log.Error("[AudioMgr] Buses must all be registered at startup.");
                return;
            }

            m_BusLateBindQueue.PushBack(bus);
        }

        private void OnAudioBusUnloaded(AudioBus bus) {
            // should never be unloaded whyyyyyy
            throw new InvalidOperationException("AudioBus instances should all be loaded at boot. They cannot be unloaded.");
        }

        #endregion // Asset Handlers

        #region Listener

        /// <summary>
        /// Sets the global AudioListener reference.
        /// </summary>
        public void SetListener(AudioListener listener) {
            m_ListenerReference = listener;
        }

        /// <summary>
        /// Sets the global AudioListener reference.
        /// </summary>
        public void RemoveListener(AudioListener listener) {
            if (m_ListenerReference == listener) {
                m_ListenerReference = null;
            }
        }

        /// <summary>
        /// Global audio listener.
        /// </summary>
        public AudioListener Listener {
            get {
                if (!m_ListenerReference) {
                    Log.Error("[AudioMgr] AudioListener reference not assigned - make sure to attach an 'AudioListenerReference' component!");
                    m_ListenerReference = Find.Any<AudioListener>();
                }
                return m_ListenerReference;
            }
        }

        #endregion // Listener

        #region Command Pipe

        internal void QueueAudioCommand(in AudioCommand cmd) {
            m_CommandPipe.Write(cmd);
        }

        internal AudioHandle QueuePlayAudioCommand(AudioCommandType cmdType, PlayCommandData cmdData) {
            UniqueId16 id = m_VoiceIdAllocator.Alloc();
            cmdData.Handle = id;
            m_CommandPipe.Write(new AudioCommand() {
                Type = cmdType
            });
            m_PlayCommandPipe.Write(cmdData);
            return new AudioHandle(id);
        }

        #endregion // Command Pipe

        #region Preload

        /// <summary>
        /// Queues the given clip to be preloaded.
        /// </summary>
        public void QueuePreload(AudioClip clip) {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) {
                m_PreloadQueue.PushBack(clip);
            }
        }

        /// <summary>
        /// Queues the given clip to be preloaded immediately.
        /// </summary>
        public void PushPreloadImmediate(AudioClip clip) {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) {
                m_PreloadQueue.PushFront(clip);
            }
        }

        #endregion // Preload

        #region Debug

        /// <summary>
        /// Gets the debug properties for a given bus.
        /// </summary>
        public AudioPropertyBlock GetDebugProperties(StringHash32 busId) {
#if DEVELOPMENT
            int busIdx = FindBusIndexForId(busId);
            if (busIdx >= 0) {
                return m_DebugBusProperties[busIdx];
            }
#endif // DEVELOPMENT
            return default;
        }

        /// <summary>
        /// Sets debug properties for a given bus.
        /// </summary>
        public void SetDebugProperties(StringHash32 busId, AudioPropertyBlock propertyBlock) {
#if DEVELOPMENT
            int busIdx = FindBusIndexForId(busId);
            if (busIdx >= 0) {
                m_DebugBusProperties[busIdx] = propertyBlock;
            }
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Resets debug properties for a given bus.
        /// </summary>
        public void ResetDebugProperties(StringHash32 busId) {
#if DEVELOPMENT
            int busIdx = FindBusIndexForId(busId);
            if (busIdx >= 0) {
                m_DebugBusProperties[busIdx] = AudioPropertyBlock.Default;
            }
#endif // DEVELOPMENT
        }

        private enum DebuggingFlags {
            TraceExecution,
            DisplayStats,
            DisplayVoiceList
        }

#if DEVELOPMENT

        [EngineMenuFactory]
        static private DMInfo CreateAudioDebugMenu() {
            DMInfo info = new DMInfo("Audio", 16);
            DebugFlags.Menu.AddSingleFrameFlagButton(info, "Trace Execution for Frame", DebuggingFlags.TraceExecution);
            info.AddDivider();
            DebugFlags.Menu.AddFlagToggle(info, "Display Stats", DebuggingFlags.DisplayStats);
            DebugFlags.Menu.AddFlagToggle(info, "Display Voices", DebuggingFlags.DisplayVoiceList);

            return info;
        }

#endif // DEVELOPMENT

        #endregion // Debug
    }
}