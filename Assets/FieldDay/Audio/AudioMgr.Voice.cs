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

        #region Voice Data

        private sealed unsafe class VoiceData {
            public UniqueId16 Handle;
            public AudioPlaybackFlags Flags;
            public StringHash32 Tag;
            public StringHash32 EventId;
            public float PlaybackDelay;
            public VoiceState State;
            public AudioPropertyBlock* EventProperties;
            public AudioPropertyBlock* VoiceProperties;
            public AudioPropertyBlock LastKnownProperties;
            public AudioVoiceComponents Components;

            public double PlayStartedTS;
            public ushort FrameEnded;
            public short PositionSyncIndex;
            public short KillTweenIndex;
            public FloatTweenIndices FloatTweens;
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
                    if (tween.Linked != UniqueId16.Invalid && tween.KillOnFinish) {
                        VoiceData voice = FindVoiceForId(tween.Linked);
                        if (voice != null) {
                            RequestImmediateStop(voice);
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

        private void UpdateVoices(float deltaTime, double currentTime) {
            for(int i = m_ActiveVoices.Count - 1; i >= 0; i--) {
                VoiceData voice = m_ActiveVoices[i];
                UpdateVoicePropertyBlock(voice, AudioPropertyBlock.Default); // TODO: Retrieve bus state

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
                        if (voice.PlaybackDelay <= 0 && IsVoiceLoaded(voice)) {
                            if ((voice.Flags & AudioPlaybackFlags.RandomizePlaybackStart) != 0) {
                                voice.Components.Source.time = RNG.Instance.NextFloat(voice.Components.Source.clip.length);
                            }
                            SyncVoiceSettings(voice);
                            ForcePositionSync(voice);
                            voice.State = VoiceState.Playing;
                            voice.PlayStartedTS = currentTime;
                            voice.Components.Source.Play();
                            Log.Debug("[AudioMgr] Playing source '{0}'", voice.Components.name);
                        }

                        break;
                    }

                    case VoiceState.Playing: {
                        if (voice.LastKnownProperties.Pause) {
                            voice.State = VoiceState.Paused;
                            voice.Components.Source.Pause();
                            break;
                        }

                        SyncVoiceSettings(voice);

                        if (voice.Components.Source.isPlaying) {
                            voice.FrameEnded = Frame.InvalidIndex;
                        } else {
                            if (voice.FrameEnded == Frame.InvalidIndex) {
                                voice.FrameEnded = Frame.Index;
                            } else if (Frame.Age(voice.FrameEnded) >= 3) {
                                voice.Components.Source.Stop();
                                voice.State = VoiceState.Stopped;
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
            components.Source.pitch = block.Pitch;
            components.Source.panStereo = block.Pan;
            components.Source.mute = block.Mute;

#if SUPPORTS_AUDIOEFFECTS

            if (components.LowPass != null) {
                components.LowPass.enabled = block.LoPass > 0;
                components.LowPass.lowpassResonanceQ = block.LoPass;
            }

            if (components.HighPass != null) {
                components.HighPass.enabled = block.HiPass > 0;
                components.HighPass.highpassResonanceQ = block.HiPass;
            }

#endif // SUPPORTS_AUDIOEFFECTS
        }

        private void ForcePositionSync(VoiceData voiceData) {
            if (voiceData.PositionSyncIndex >= 0) {
                ForceSyncEmitterLocation(m_PositionSyncTable[voiceData.PositionSyncIndex]);
            }
        }

        static private void RequestImmediateStop(VoiceData voice) {
            voice.Components.Source.Stop();
            voice.State = VoiceState.Stopped;
        }

        static private bool IsVoiceLoaded(VoiceData voice) {
            return voice.Components.Source.clip.loadState == AudioDataLoadState.Loaded;
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
            int score = (int) (voice.LastKnownProperties.Volume * 100);

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
            voice.Components.Source.Stop();
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

            voice.Components = null;
            voice.Handle = default;
            voice.PlayStartedTS = -1;
            voice.FrameEnded = Frame.InvalidIndex;

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

            AudioSource source = go.AddComponent<AudioSource>();
            source.enabled = false;
            source.playOnAwake = false;

#if SUPPORTS_AUDIOEFFECTS

            go.AddComponent<AudioLowPassFilter>().enabled = false;
            go.AddComponent<AudioHighPassFilter>().enabled = false;

#endif // SUPPORTS_AUDIOEFFECTS

            AudioVoiceComponents voiceComponents = go.AddComponent<AudioVoiceComponents>();
            voiceComponents.Sync();

            go.SetActive(false);
            return voiceComponents;
        }

        #endregion // Voice Component Pool
    }
}