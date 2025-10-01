#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Files;
using System.IO;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        private void FlushCommandPipe() {
            while(m_CommandPipe.TryRead(out AudioCommand cmd)) {
                switch (cmd.Type) {
                    case AudioCommandType.StopAll: {
                        Cmd_StopAll();
                        break;
                    }

                    case AudioCommandType.StopWithHandle: {
                        Cmd_StopWithHandle(cmd.Stop.Id.Handle, cmd.Stop.FadeOut, cmd.Stop.FadeOutCurve);
                        break;
                    }

                    case AudioCommandType.StopWithAudioSource: {
                        Cmd_StopForAudioSource(UnityHelper.Find<AudioSource>(cmd.Stop.Id.InstanceId), cmd.Stop.FadeOut, cmd.Stop.FadeOutCurve);
                        break;
                    }

                    case AudioCommandType.StopWithTag: {
                        Cmd_StopWithTag(cmd.Stop.Id.Id, cmd.Stop.FadeOut, cmd.Stop.FadeOutCurve);
                        break;
                    }

                    case AudioCommandType.SetTagWithHandle: {
                        Cmd_SetTagForHandle(cmd.SetTag);
                        break;
                    }

                    case AudioCommandType.SetVoiceBoolParameter: {
                        Cmd_SetVoiceBoolParameter(cmd.BoolParam);
                        break;
                    }

                    case AudioCommandType.SetVoiceFloatParameter: {
                        Cmd_SetVoiceFloatParameter(cmd.FloatParam);
                        break;
                    }

                    case AudioCommandType.SetBusBoolParameter: {
                        Cmd_SetBusBoolParameter(cmd.BoolParam);
                        break;
                    }

                    case AudioCommandType.SetBusFloatParameter: {
                        Cmd_SetBusFloatParameter(cmd.FloatParam);
                        break;
                    }

                    case AudioCommandType.SetBusConfigVolume: {
                        Cmd_SetBusConfigVolume(cmd.ConfigVolume);
                        break;
                    }

                    case AudioCommandType.PlayClipFromName: {
                        Cmd_PlayFromName(m_PlayCommandPipe.Read());
                        break;
                    }

                    case AudioCommandType.PlayClipFromAssetRef: {
                        Cmd_PlayFromAsset(m_PlayCommandPipe.Read());
                        break;
                    }

                    case AudioCommandType.PlayFromHandle: {
                        Cmd_PlayExisting(cmd.Resume.Handle);
                        break;
                    }

                    case AudioCommandType.Seek: {
                        Cmd_Seek(cmd.Seek);
                        break;
                    }

                    case AudioCommandType.SetMixState: {
                        Cmd_SetMixState(cmd.SetMixState);
                        break;
                    }

                    case AudioCommandType.SetLoop: {
                        Cmd_SetLoop(cmd.SetLoop);
                        break;
                    }

                    default: {
                        Log.Error("[AudioMgr] Unknown audio command type '{0}'", cmd.Type);
                        break;
                    }
                }
            }
        }

        #region Stop

        private void Cmd_StopAll() {
            for(int i = m_ActiveVoices.Count - 1; i >= 0; i--) {
                KillVoice(m_ActiveVoices[i]);
            }

            m_ActiveVoices.Clear();
        }

        private unsafe void Cmd_StopWithHandle(UniqueId16 handle, float delay, Curve curve) {
            VoiceData voice = FindVoiceForId(handle, out int idx);
            if (idx >= 0) {
                if (delay <= 0 || voice.State == VoiceState.PlayRequested) {
                    KillVoice(voice);
                    m_ActiveVoices.FastRemoveAt(idx);
                } else {
                    FreeTween(ref voice.KillTweenIndex, handle);

                    FloatParamTweenData tween;
                    tween.Source = voice.EventProperties;
                    tween.Start = voice.EventProperties->Volume;
                    tween.Delta = -tween.Start;
                    tween.InvDeltaTime = 1f / delay;
                    tween.Progress = 0;
                    tween.Property = AudioFloatPropertyType.Volume;
                    tween.Curve = curve;
                    tween.Linked = handle;
                    tween.KillOnFinish = true;

                    voice.KillTweenIndex = (short) m_FloatTweenTable.PushBack(ref m_FloatTweenList, tween);
                }
            }
        }

        private unsafe void Cmd_StopForAudioSource(AudioSource source, float delay, Curve curve) {
            if (source != null && source.TryGetComponent<AudioVoiceComponents>(out var voiceComponents)) {
                if (voiceComponents.PlayingHandle.Id != 0) {
                    Cmd_StopWithHandle(voiceComponents.PlayingHandle, delay, curve);
                }
            }
        }

        private unsafe void Cmd_StopWithTag(StringHash32 tag, float delay, Curve curve) {
            for (int i = m_ActiveVoices.Count - 1; i >= 0; i--) {
                VoiceData voice = m_ActiveVoices[i];
                if (voice.Tag == tag) {
                    if (delay <= 0 || voice.State == VoiceState.PlayRequested) {
                        KillVoice(voice);
                        m_ActiveVoices.FastRemoveAt(i);
                    } else {
                        FreeTween(ref voice.KillTweenIndex, voice.Handle);

                        FloatParamTweenData tween;
                        tween.Source = voice.EventProperties;
                        tween.Start = voice.EventProperties->Volume;
                        tween.Delta = -tween.Start;
                        tween.InvDeltaTime = 1f / delay;
                        tween.Progress = 0;
                        tween.Property = AudioFloatPropertyType.Volume;
                        tween.Curve = curve;
                        tween.Linked = voice.Handle;
                        tween.KillOnFinish = true;

                        voice.KillTweenIndex = (short) m_FloatTweenTable.PushBack(ref m_FloatTweenList, tween);
                    }
                }
            }
        }

        #endregion // Stop

        #region Params

        private unsafe void Cmd_SetTagForHandle(OverwriteTagCommandData tagChange) {
            VoiceData voice = FindVoiceForId(tagChange.Handle);
            if (voice != null) {
                voice.Tag = tagChange.Tag;
            }
        }

        private unsafe void Cmd_SetVoiceBoolParameter(BoolParamChangeCommandData paramChange) {
            VoiceData voice = FindVoiceForId(paramChange.Handle.Handle);
            if (voice != null) {
                (*voice.VoiceProperties).SetBool(paramChange.Property, paramChange.Target);
            }
        }

        private unsafe void Cmd_SetVoiceFloatParameter(FloatParamChangeCommandData paramChange) {
            VoiceData voice = FindVoiceForId(paramChange.Handle.Handle);
            if (voice != null) {
                FreeTween(ref voice.FloatTweens.Indices[(int) paramChange.Property], voice.Handle);

                if (paramChange.Duration <= 0) {
                    voice.VoiceProperties->SetFloat(paramChange.Property, paramChange.Target);
                } else {
                    FloatParamTweenData tween;
                    tween.Source = voice.VoiceProperties;
                    tween.Start = voice.VoiceProperties->GetFloat(paramChange.Property);
                    tween.Delta = paramChange.Target - tween.Start;
                    tween.InvDeltaTime = 1f / paramChange.Duration;
                    tween.Progress = 0;
                    tween.Property = paramChange.Property;
                    tween.Curve = paramChange.Easing;
                    tween.Linked = voice.Handle;
                    tween.KillOnFinish = false;

                    voice.FloatTweens.Indices[(int) paramChange.Property] = (short) m_FloatTweenTable.PushBack(ref m_FloatTweenList, tween);
                }
            }
        }

        private unsafe void Cmd_SetBusBoolParameter(BoolParamChangeCommandData paramChange) {
            ref BusData bus = ref FindBusForId(paramChange.Handle.BusId);
            if (!Unsafe.IsNullRef(ref bus)) {
                (*bus.ScriptProperties).SetBool(paramChange.Property, paramChange.Target);
            }
        }

        private unsafe void Cmd_SetBusFloatParameter(FloatParamChangeCommandData paramChange) {
            ref BusData bus = ref FindBusForId(paramChange.Handle.BusId);
            if (!Unsafe.IsNullRef(ref bus)) {
                FreeTween(ref bus.FloatTweens.Indices[(int) paramChange.Property], bus.Handle);

                if (paramChange.Duration <= 0) {
                    bus.ScriptProperties->SetFloat(paramChange.Property, paramChange.Target);
                } else {
                    FloatParamTweenData tween;
                    tween.Source = bus.ScriptProperties;
                    tween.Start = bus.ScriptProperties->GetFloat(paramChange.Property);
                    tween.Delta = paramChange.Target - tween.Start;
                    tween.InvDeltaTime = 1f / paramChange.Duration;
                    tween.Progress = 0;
                    tween.Property = paramChange.Property;
                    tween.Curve = paramChange.Easing;
                    tween.Linked = bus.Handle;
                    tween.KillOnFinish = false;

                    bus.FloatTweens.Indices[(int) paramChange.Property] = (short) m_FloatTweenTable.PushBack(ref m_FloatTweenList, tween);
                }
            }
        }

        private unsafe void Cmd_SetBusConfigVolume(ConfigVolumeChangeCommandData volumeChange) {
            ref BusData bus = ref FindBusForId(volumeChange.BusId);
            if (!Unsafe.IsNullRef(ref bus)) {
                bus.ConfigVolume = volumeChange.Target;
            }
        }

        private unsafe void Cmd_Seek(SeekCommandData seekData) {
            VoiceData voice = FindVoiceForId(seekData.Handle);
            if (voice != null) {
                voice.Components.Source.time = seekData.Position;
            }
        }

        private unsafe void Cmd_SetLoop(SetLoopCommandData loopData) {
            VoiceData voice = FindVoiceForId(loopData.Handle);
            if (voice != null) {
                voice.Components.Source.loop = loopData.Loop;
            }
        }

        #endregion // Params

        #region Playback

        private void Cmd_PlayFromName(PlayCommandData cmd) {
            if (!Game.Assets.TryGetNamed(cmd.Asset.AssetId, out AudioEvent evt)) {
                Log.Warn("[AudioMgr] No AudioEvent loaded with name '{0}'", cmd.Asset.AssetId.ToDebugString());
                FreeHandle(ref cmd.Handle);
                return;
            }

            PlayClipInternal(cmd, evt, null);
        }

        private void Cmd_PlayFromAsset(PlayCommandData cmd) {
            var asset = Find.FromId(cmd.Asset.InstanceId);
            AudioClip clip = asset as AudioClip;

            if (clip == null) {
                Log.Error("[AudioMgr] No clips found with instance id '{0}'", cmd.Asset.InstanceId);
                FreeHandle(ref cmd.Handle);
                return;
            }

            PlayClipInternal(cmd, null, clip);
        }

        private void Cmd_PlayExisting(UniqueId16 id) {
            VoiceData voice = FindVoiceForId(id);
            if (voice != null && voice.KillTweenIndex < 0) {
                if (voice.State == VoiceState.Idle) {
                    voice.State = VoiceState.PlayRequested;
                }
            }
        }

        private unsafe void PlayClipInternal(PlayCommandData cmd, AudioEvent evt, AudioClip clip) {
            float delay = cmd.Delay;
            byte priority = 128;
            float pan = cmd.Pan;

            AudioPropertyBlock evtProperties = AudioPropertyBlock.Default;
            StreamedClip streamedClip = null;

            if (clip == null && (cmd.Flags & AudioPlaybackFlags.SecondaryClipOverride) != 0) {
                clip = Find.FromId(cmd.SecondaryAsset.InstanceId) as AudioClip;
            }
            
            if (evt != null) {
                if (clip == null) {
                    if (evt.CachedStreamedClipKey != 0) {
                        streamedClip = GetStreamedClip(evt.CachedStreamedClipKey);
                        clip = streamedClip.Clip;
                    } else {
                        if (evt.SampleSelector == null) {
                            evt.SampleSelector = new RandomDeck<AudioClip>(evt.Samples);
                        }
                        clip = evt.SampleSelector.Next();
                    }
                }

                evtProperties.Volume = evt.Volume.Generate() * evt.VolumeMultiplier;
                evtProperties.Pitch = evt.Pitch.Generate();
                evtProperties.Pan = evt.Pan.Generate();

                if (evt.RandomizePanSign && RNG.Instance.NextBool()) {
                    evtProperties.Pan = -evtProperties.Pan;
                }

                delay += evt.Delay.Generate();
                pan += evtProperties.Pan;

                if (evt.Loop) {
                    cmd.Flags |= AudioPlaybackFlags.Loop;

                    if (evt.RandomizeStartTime) {
                        cmd.Flags |= AudioPlaybackFlags.RandomizePlaybackStart;
                    }
                }

                if (cmd.Tag.IsEmpty) {
                    cmd.Tag = evt.Tag;
                }

                priority = evt.Priority;
            }

            if (clip == null && (streamedClip == null || (streamedClip.Flags & StreamedClipFlags.Error) != 0)) {
                Log.Error("[AudioMgr] Failed to resolve clip");
                FreeHandle(ref cmd.Handle);
                return;
            }

            AudioEmitterConfig emitterConfig;
            if (evt != null && !evt.EmitterConfiguration.IsEmpty) {
                if (evt.CachedEmitterProfile == null) {
                    evt.CachedEmitterProfile = Find.NamedAsset<AudioEmitterProfile>(evt.EmitterConfiguration);
                }
                emitterConfig = evt.CachedEmitterProfile.Config;
            } else {
                emitterConfig = m_DefaultEmitterConfig;
            }

            // randomize playback start is not compatible with non-looped sounds
            if ((cmd.Flags & AudioPlaybackFlags.Loop) == 0) {
                cmd.Flags &= ~AudioPlaybackFlags.RandomizePlaybackStart;
            }

            EnsureFreeVoice();

            UnityEngine.Object providedObject = Find.FromId(cmd.TransformOrAudioSourceId);

            Transform playbackPos = providedObject as Transform;
            AudioVoiceComponents voiceComponents;
            AudioSource src;
            if ((cmd.Flags & AudioPlaybackFlags.UseProvidedSource) != 0) {
                src = providedObject as AudioSource;
                Assert.True(src != null, "UserProvidedSource flag set but AudioSource not sent alongside it");
                Cmd_StopForAudioSource(src, 0, Curve.Linear); // ensure only one voice is playing
                voiceComponents = src.EnsureComponent<AudioVoiceComponents>();
                voiceComponents.Sync();
            } else {
                voiceComponents = m_VoiceComponentPool.Alloc();
                src = voiceComponents.Source;
            }

            src.clip = clip;
            src.priority = priority;
            src.loop = (cmd.Flags & AudioPlaybackFlags.Loop) != 0;

            if (streamedClip != null) {
                if ((streamedClip.Flags & StreamedClipFlags.LoadingStateMask) == 0) {
                    LoadStreamed(streamedClip, delay > 0 ? FileLoadPriority.High : FileLoadPriority.Urgent);
                }
            } else {
                if (clip.loadState == AudioDataLoadState.Unloaded) {
                    if (delay > 0) {
                        m_PreloadQueue.PushBack(clip);
                    } else {
                        m_PreloadQueue.PushFront(clip);
                    }
                }
            }

            AudioEmitterConfig.ApplyConfiguration(src, emitterConfig, m_HasSpatializationPlugin);
            voiceComponents.enabled = true;
            voiceComponents.Source.enabled = true;

            VoiceData voice = AllocateVoice(cmd.Handle);
            voice.Flags = cmd.Flags;
            voice.Tag = cmd.Tag;
            voice.PlaybackDelay = delay;
            voice.State = VoiceState.PlayRequested;
            voice.Components = voiceComponents;
            voice.Components.PlayingHandle = cmd.Handle;

            *voice.EventProperties = evtProperties;
            voice.VoiceProperties->Volume = cmd.Volume;
            voice.VoiceProperties->Pitch = cmd.Pitch;
            voice.VoiceProperties->Pan = cmd.Pan;

            voice.EventId = evt ? evt.CachedId : default;
            voice.BusIndex = evt ? evt.CachedBusIndex : 0;

#if DEVELOPMENT
            if (clip != null) {
                voice.DebugName = clip.name;
            } else {
                voice.DebugName = Path.GetFileNameWithoutExtension(streamedClip.Path);
            }
#endif // DEVELOPMENT

            voice.StreamingEntry = streamedClip;
            if (streamedClip != null) {
                streamedClip.RefCount++;
                Assert.True(streamedClip.RefCount != 0, "Too many references to streamed clip");
            }

#if UNITY_EDITOR
            if ((cmd.Flags & AudioPlaybackFlags.UseProvidedSource) == 0) {
                voiceComponents.gameObject.name = voice.DebugName;
            }
#endif // UNITY_EDITOR

            if ((cmd.Flags & AudioPlaybackFlags.UseProvidedSource) == 0 && emitterConfig.Mode != AudioEmitterMode.Fixed) {
                if (playbackPos) {
                    PositionSyncData posSync;
                    posSync.EmitterPosition = voiceComponents.transform;
                    posSync.Reference = playbackPos;
                    posSync.RefOffset = cmd.TransformOffset;
                    posSync.RefRotation = cmd.RotationOffset;
                    posSync.RefOffsetSpace = cmd.TransformOffsetSpace;
                    posSync.Mapping = emitterConfig.Mode;
                    voice.PositionSyncIndex = (short) m_PositionSyncTable.PushBack(ref m_PositionSyncList, posSync);
                } else {
                    voiceComponents.transform.SetPositionAndRotation(cmd.TransformOffset, cmd.RotationOffset);
                }
            }

            m_ActiveVoices.PushBack(voice);
        }

        #endregion // Playback

        #region Mixes

        private unsafe void Cmd_SetMixState(SetMixStateData mixChange) {
            SetMixStateTarget(mixChange.MixId, mixChange.Target, mixChange.Duration, mixChange.Proportional, mixChange.UseDefaultEnvelope);
        }

        #endregion // Mixes
    }
}