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
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        private const int FloatPropertyCount = 5;

        private const float MinLowHighPassCutoff = 17;
        private const float MaxLowHighPassCutoff = 20000;
        private const float LowHighPassCutoffRange = MaxLowHighPassCutoff - MinLowHighPassCutoff;

        private const float MinPitch =
#if UNITY_WEBGL
            0.07f;
#else
            0;
#endif // UNITY_WEBGL

        private const float MaxPitch =
#if UNITY_EDITOR
            64;
#elif UNITY_WEBGL
            8;
#else
            16;
#endif // UNITY_WEBGL

        #region Voice Data

        private sealed unsafe class VoiceData {
            public UniqueId16 Handle;
            public AudioPlaybackFlags Flags;
            public StringHash32 Tag;
            public StringHash32 EventId;
            public float PlaybackDelay;
            public VoiceState State;
            public int BusIndex;
            public AudioPropertyBlock* EventProperties;
            public AudioPropertyBlock* VoiceProperties;
            public AudioPropertyBlock LastKnownProperties;
            public AudioVoiceComponents Components;

            public double PlayStartedTS;
            public ushort FrameEnded;
            public short PositionSyncIndex;
            public short KillTweenIndex;
            public FloatTweenIndices FloatTweens;
            public StreamedClip StreamingEntry;

#if DEVELOPMENT
            public string DebugName;
#endif // DEVELOPMENT
        }

        private enum VoiceState : byte {
            Idle,
            PlayRequested,
            Playing,
            Paused,
            Stopped
        }

        private unsafe struct FloatTweenIndices {
            public fixed short Indices[FloatPropertyCount];

            public void Reset() {
                for(int i = 0; i < FloatPropertyCount; i++) {
                    Indices[i] = -1;
                }
            }
        }

        private struct PositionSyncData {
            public Transform EmitterPosition;
            public Transform Reference;
            public Vector3 RefOffset;
            public Quaternion RefRotation;
            public Space RefOffsetSpace;
            public AudioEmitterMode Mapping;
        }

        private unsafe struct FloatParamTweenData {
            public AudioPropertyBlock* Source;
            public AudioFloatPropertyType Property;
            public Curve Curve;
            public UniqueId16 Linked;
            public float Start;
            public float Delta;
            public float InvDeltaTime;
            public float Progress;
            public bool KillOnFinish;
        }

        #endregion // Voice Data

        #region Position Sync

        private void SyncEmitterLocations() {
            if (m_PositionSyncList.Length <= 0) {
                return;
            }

            var enumerator = m_PositionSyncTable.GetEnumerator(m_PositionSyncList);
            while(enumerator.MoveNext()) {
                ForceSyncEmitterLocation(enumerator.Current.Tag);
            }
        }

        static private void ForceSyncEmitterLocation(PositionSyncData data) {
            if (!data.Reference) {
                return;
            }

            data.Reference.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            if (IsNonDefault(data.RefOffset)) {
                switch (data.RefOffsetSpace) {
                    case Space.Self: {
                        pos += data.Reference.TransformVector(data.RefOffset);
                        break;
                    }
                    case Space.World: {
                        pos += data.RefOffset;
                        break;
                    }
                }
            }

            if (IsNonDefault(data.RefRotation)) {
                rot = rot * data.RefRotation;
            }

            // TODO: Implement mapping

            data.EmitterPosition.SetPositionAndRotation(pos, rot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsNonDefault(Vector3 pos) {
            return pos.x != 0 || pos.y != 0 || pos.z != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsNonDefault(Quaternion rot) {
            return rot.x != 0 || rot.y != 0 || rot.z != 0 || rot.w != 1;
        }

        #endregion // Position Sync

        #region Tween Update

        private unsafe void UpdateTweens(float deltaTime) {
            if (m_FloatTweenList.Length <= 0) {
                return;
            }

            var enumerator = m_FloatTweenTable.GetEnumerator(m_FloatTweenList);
            while(enumerator.MoveNext()) {
                ref FloatParamTweenData tween = ref m_FloatTweenTable[enumerator.Current.Index];
                float finalProgress = tween.Progress = Math.Min(1f, tween.Progress + deltaTime * tween.InvDeltaTime);

                float newVal = tween.Start + tween.Delta * TweenUtil.Evaluate(tween.Curve, finalProgress);
                tween.Source->SetFloat(tween.Property, newVal);

                if (finalProgress >= 1) {
                    if (tween.Linked != UniqueId16.Invalid) {
                        VoiceData voice = FindVoiceForId(tween.Linked);
                        if (voice != null) {
                            if (tween.KillOnFinish) {
                                voice.KillTweenIndex = -1;
                                RequestImmediateStop(voice);
                            } else {
                                voice.FloatTweens.Indices[(int) tween.Property] = -1;
                            }
                        }
                    }

                    m_FloatTweenTable.Remove(ref m_FloatTweenList, enumerator.Current.Index);
                }
            }
        }

        #endregion // Tween Update

        #region Voice Update

        private void EnsureFreeVoice() {
            if (m_VoiceDataPool.Count == 0) {
                FreeUpVoice(Time.realtimeSinceStartupAsDouble);
                Assert.True(m_VoiceDataPool.Count > 0);
            }
        }

        private unsafe VoiceData AllocateVoice(UniqueId16 handle) {
            VoiceData data = m_VoiceDataPool.Alloc();
            data.Handle = handle;

            data.EventProperties = m_TargetablePropertyBlocks.Alloc();
            data.VoiceProperties = m_TargetablePropertyBlocks.Alloc();

            *data.EventProperties = AudioPropertyBlock.Default;
            *data.VoiceProperties = AudioPropertyBlock.Default;

            data.State = VoiceState.Idle;
            data.PlaybackDelay = 0;
            data.PositionSyncIndex = -1;
            data.KillTweenIndex = -1;
            data.FloatTweens.Reset();

            return data;
        }

        private VoiceData FindVoiceForId(UniqueId16 id, out int index) {
            if (m_VoiceIdAllocator.IsValid(id)) {
                for (int i = 0; i < m_ActiveVoices.Count; i++) {
                    if (m_ActiveVoices[i].Handle == id) {
                        index = i;
                        return m_ActiveVoices[i];
                    }
                }
            }

            index = -1;
            return null;
        }

        private VoiceData FindVoiceForId(UniqueId16 id) {
            if (m_VoiceIdAllocator.IsValid(id)) {
                for (int i = 0; i < m_ActiveVoices.Count; i++) {
                    if (m_ActiveVoices[i].Handle == id) {
                        return m_ActiveVoices[i];
                    }
                }
            }

            return null;
        }

        private int CullFinishedVoices() {
            int culled = 0;
            for (int i = m_ActiveVoices.Count - 1; i >= 0; i--) {
                VoiceData voice = m_ActiveVoices[i];
                if (voice.State == VoiceState.Stopped) {
                    KillVoice(voice);
                    m_ActiveVoices.FastRemoveAt(i);
                    culled++;
                }
            }
            return culled;
        }

        private unsafe void UpdateVoices(float deltaTime, double currentTime) {
            AudioPropertyBlock* busValues = stackalloc AudioPropertyBlock[m_BusCount];
            for(int i = 0; i < m_BusCount; i++) {
                busValues[i] = m_WorkingBusProperties[i];
            }

            for(int i = m_ActiveVoices.Count - 1; i >= 0; i--) {
                VoiceData voice = m_ActiveVoices[i];
                UpdateVoicePropertyBlock(voice, busValues[voice.BusIndex]);

                switch (voice.State) {
                    case VoiceState.Idle: {
                        break;
                    }

                    case VoiceState.Stopped: {
                        break;
                    }

                    case VoiceState.PlayRequested: {
                        if (voice.LastKnownProperties.Pause) {
                            break;
                        }

                        if (voice.PlaybackDelay > 0) {
                            voice.PlaybackDelay -= deltaTime;
                        }

                        bool voiceLoaded = IsVoiceLoaded(voice);
                        if (!voiceLoaded && voice.StreamingEntry != null) {
                            if ((voice.StreamingEntry.Flags & StreamedClipFlags.Error) != 0) {
                                // voice failed to load
                                Log.Error("[AudioMgr] Cancelling voice due to loading error");
                                voice.State = VoiceState.Stopped;
                                UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, false);
                            } else if ((voice.StreamingEntry.Flags & StreamedClipFlags.Loaded) != 0) {
                                voice.Components.Source.clip = voice.StreamingEntry.Clip;
                                voiceLoaded = IsVoiceLoaded(voice);
                            }
                        }

                        if (voiceLoaded && voice.PlaybackDelay <= 0) {
                            if ((voice.Flags & AudioPlaybackFlags.RandomizePlaybackStart) != 0) {
                                voice.Components.Source.time = RNG.Instance.NextFloat(voice.Components.Source.clip.length);
                            }
                            SyncVoiceSettings(voice);
                            ForcePositionSync(voice);
                            voice.State = VoiceState.Playing;
                            voice.PlayStartedTS = currentTime;
                            voice.Components.Source.Play();
                            UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, true);
                        }

                        break;
                    }

                    case VoiceState.Playing: {
                        if (voice.LastKnownProperties.Pause) {
                            voice.State = VoiceState.Paused;
                            voice.Components.Source.Pause();
                            UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, false);
                            break;
                        }

                        SyncVoiceSettings(voice);

                        if (voice.Components.Source.isPlaying) {
                            voice.FrameEnded = Frame.InvalidIndex;
                        } else {
                            if (voice.Components.Source.loop) {
                                voice.State = VoiceState.PlayRequested;
                            } else if (voice.FrameEnded == Frame.InvalidIndex) {
                                voice.FrameEnded = Frame.Index;
                            } else if (Frame.Age(voice.FrameEnded) >= 8) {
                                voice.Components.Source.Stop();
                                voice.State = VoiceState.Stopped;
                                UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, false);
                            }
                        }

                        break;
                    }

                    case VoiceState.Paused: {
                        if (!voice.LastKnownProperties.Pause) {
                            SyncVoiceSettings(voice);
                            ForcePositionSync(voice);
                            voice.State = VoiceState.Playing;
                            voice.Components.Source.UnPause();
                            UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, true);
                        }
                        break;
                    }
                }
            }
        }

        static private unsafe void UpdateVoicePropertyBlock(VoiceData voiceData, in AudioPropertyBlock parentProperties) {
            AudioPropertyBlock block = parentProperties;
            AudioPropertyBlock.Combine(block, *voiceData.EventProperties, ref block);
            AudioPropertyBlock.Combine(block, *voiceData.VoiceProperties, ref block);
            voiceData.LastKnownProperties = block;
        }

        static private unsafe void SyncVoiceSettings(VoiceData voiceData) {
            AudioPropertyBlock block = voiceData.LastKnownProperties;
            AudioVoiceComponents components = voiceData.Components;

            components.Source.volume = block.Volume;
            components.Source.pitch = Math.Min(MaxPitch, Math.Max(MinPitch, block.Pitch));
            components.Source.panStereo = block.Pan;
            components.Source.mute = block.Mute;

#if SUPPORTS_AUDIOEFFECTS

            if (components.LowPass != null) {
                components.LowPass.enabled = block.LoPass > 0;
                if (block.LoPass > 0) {
                    components.LowPass.cutoffFrequency = CalculateCutoffFrequency(1f - block.LoPass);
                }
            }

            if (components.HighPass != null) {
                components.HighPass.enabled = block.HiPass > 0;
                if (block.HiPass > 0) {
                    components.HighPass.cutoffFrequency = CalculateCutoffFrequency(block.HiPass);
                }
            }

#endif // SUPPORTS_AUDIOEFFECTS
        }

        private void ForcePositionSync(VoiceData voiceData) {
            if (voiceData.PositionSyncIndex >= 0) {
                ForceSyncEmitterLocation(m_PositionSyncTable[voiceData.PositionSyncIndex]);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RequestImmediateStop(VoiceData voice) {
            voice.Components.Source.Stop();
            voice.State = VoiceState.Stopped;
            UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool IsVoiceLoaded(VoiceData voice) {
            AudioClip clip = voice.Components.Source.clip;
            return clip != null && clip.loadState == AudioDataLoadState.Loaded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float CalculateCutoffFrequency(float value) {
            return MinLowHighPassCutoff + LowHighPassCutoffRange * (value * value * value);
        }

        #endregion // Voice Update

        #region Voice Queries

        internal bool WasVoiceAudible(AudioHandle handle) {
            var voice = FindVoiceForId(handle.m_Id);
            if (voice != null) {
                return voice.State == VoiceState.Playing && voice.LastKnownProperties.IsAudible();
            }
            return false;
        }

        internal bool IsVoiceActive(AudioHandle handle) {
            return m_VoiceIdAllocator.IsValid(handle.m_Id);
        }

        internal AudioSource GetVoiceSource(AudioHandle handle) {
            var voice = FindVoiceForId(handle.m_Id);
            if (voice != null) {
                return voice.Components.Source;
            }
            return null;
        }

        #endregion // Voice Queries

        #region Cleanup

        private void FreeUpVoice(double currentTime) {
            Assert.True(m_ActiveVoices.Count > 0);
            
            int finishedCull = CullFinishedVoices();
            if (finishedCull > 0) {
                return;
            }

            int greatestIdx = 0;
            int greatestScore = CalculateKillPriorityScore(m_ActiveVoices[0], currentTime);

            for(int i = 1; i < m_ActiveVoices.Count; i++) {
                int checkScore = CalculateKillPriorityScore(m_ActiveVoices[i], currentTime);
                if (checkScore > greatestScore) {
                    greatestIdx = i;
                    greatestScore = checkScore;
                }
            }

            KillVoice(m_ActiveVoices[greatestIdx]);
            m_ActiveVoices.FastRemoveAt(greatestIdx);
        }

        static private unsafe int CalculateKillPriorityScore(VoiceData voice, double currentTime) {
            int score = 101 - (int) (voice.LastKnownProperties.Volume * 100);

            switch (voice.State) {
                case VoiceState.Playing:
                case VoiceState.Paused: {
                    score = (int) (score + (currentTime - voice.PlayStartedTS));
                    break;
                }
                case VoiceState.Stopped: {
                    score *= 1000;
                    break;
                }
                case VoiceState.PlayRequested: {
                    score /= 2;
                    break;
                }
            }

            // if not audible, or volume is really low, boost score
            if (!voice.LastKnownProperties.IsAudible() || voice.LastKnownProperties.Volume < 0.1f) {
                score *= 5;
            }

            // manual pauses and mutes should be respected
            if (voice.VoiceProperties->Pause || voice.VoiceProperties->Mute) {
                score /= 2;
            }

            // if looping, or using a provided audio source, killing would be more detrimental to experience
            if ((voice.Flags & (AudioPlaybackFlags.Loop | AudioPlaybackFlags.UseProvidedSource)) != 0) {
                score /= 4;
            }

            score = score * voice.Components.Source.priority / 255;

            return score;
        }

        private unsafe void KillVoice(VoiceData voice) {
            if (voice.Components && voice.Components.Source) {
                voice.Components.Source.Stop();
                voice.Components.Source.clip = null;
                UpdatePlayingInstanceCount(voice.Handle, voice.BusIndex, false);
            }
            voice.Components.PlayingHandle = default;

            FreeHandle(ref voice.Handle);
            FreePositionSync(ref voice.PositionSyncIndex);
            FreeTween(ref voice.KillTweenIndex, voice.Handle);
            for(int i = 0; i < FloatPropertyCount; i++) {
                FreeTween(ref voice.FloatTweens.Indices[i], voice.Handle);
            }

            // if this is not emitting from a custom source, free it
            if ((voice.Flags & AudioPlaybackFlags.UseProvidedSource) == 0) {
                m_VoiceComponentPool.Free(voice.Components);
            }

            m_TargetablePropertyBlocks.TryFree(ref voice.EventProperties);
            m_TargetablePropertyBlocks.TryFree(ref voice.VoiceProperties);

            if (voice.StreamingEntry != null) {
                Assert.True(voice.StreamingEntry.RefCount > 0);
                voice.StreamingEntry.RefCount--;
                voice.StreamingEntry = null;
            }

            voice.Components = null;
            voice.Handle = default;
            voice.PlayStartedTS = -1;
            voice.FrameEnded = Frame.InvalidIndex;

#if DEVELOPMENT
            voice.DebugName = null;
#endif // DEVELOPMENT

            m_VoiceDataPool.Free(voice);
        }

        private void FreeHandle(ref UniqueId16 handle) {
            m_VoiceIdAllocator.Free(handle);
            handle = default;
        }

        private void FreeTween(ref short index, UniqueId16 eventId) {
            if (index >= 0) {
                if (m_FloatTweenTable[index].Linked == eventId) {
                    m_FloatTweenTable.Remove(ref m_FloatTweenList, index);
                }
                index = -1;
            }
        }

        private void FreePositionSync(ref short index) {
            if (index >= 0) {
                m_PositionSyncTable.Remove(ref m_PositionSyncList, index);
                index = -1;
            }
        }

        #endregion // Cleanup

        #region Voice Component Pool

        private AudioVoiceComponents ConstructNewSource(IPool<AudioVoiceComponents> p) {
            GameObject go = new GameObject("unused audio voice");
            go.transform.SetParent(m_AudioSourceRoot.transform);
            go.hideFlags = HideFlags.DontSave;

            AudioSource source = go.AddComponent<AudioSource>();
            source.enabled = false;
            source.playOnAwake = false;

#if SUPPORTS_AUDIOEFFECTS

            go.AddComponent<AudioLowPassFilter>().enabled = false;
            go.AddComponent<AudioHighPassFilter>().enabled = false;

#endif // SUPPORTS_AUDIOEFFECTS

            AudioVoiceComponents voiceComponents = go.AddComponent<AudioVoiceComponents>();
            voiceComponents.Sync();

            return voiceComponents;
        }

        #endregion // Voice Component Pool
    }
}