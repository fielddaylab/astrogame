using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
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

                    case AudioCommandType.SetVoiceBoolParameter: {
                        Cmd_SetVoiceBoolParameter(cmd.BoolParam);
                        break;
                    }

                    case AudioCommandType.SetVoiceFloatParameter: {
                        Cmd_SetVoiceFloatParameter(cmd.FloatParam);
                        break;
                    }

                    case AudioCommandType.PlayClipFromName: {
                        Cmd_PlayFromName(cmd.Play);
                        break;
                    }

                    case AudioCommandType.PlayClipFromAssetRef: {
                        Cmd_PlayFromAsset(cmd.Play);
                        break;
                    }

                    case AudioCommandType.PlayFromHandle: {
                        Cmd_PlayExisting(cmd.Resume.Handle);
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
                if (delay <= 0) {
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
                    if (delay <= 0) {
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

        private unsafe void Cmd_SetVoiceBoolParameter(BoolParamChangeCommandData paramChange) {
            VoiceData voice = FindVoiceForId(paramChange.Handle);
            if (voice != null) {
                (*voice.VoiceProperties).SetBool(paramChange.Property, paramChange.Target);
            }
        }

        private unsafe void Cmd_SetVoiceFloatParameter(FloatParamChangeCommandData paramChange) {
            VoiceData voice = FindVoiceForId(paramChange.Handle);
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

        // TODO: Setting bus parameters

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
            AudioEvent evt = asset as AudioEvent;

            if (clip == null && evt == null) {
                Log.Error("[AudioMgr] No clips or AudioEvents found with instance id '{0}'", cmd.Asset.InstanceId);
                FreeHandle(ref cmd.Handle);
                return;
            }

            PlayClipInternal(cmd, evt, clip);
        }

        private void Cmd_PlayExisting(UniqueId16 id) {
            VoiceData voice = FindVoiceForId(id);
            if (voice != null && voice.KillTweenIndex < 0) {
                if (voice.State == VoiceState.Idle) {
                    voice.State = VoiceState.Playing;
                }
            }
        }

        private unsafe void PlayClipInternal(PlayCommandData cmd, AudioEvent evt, AudioClip clip) {
            float delay = 0;
            byte priority = 128;

            AudioPropertyBlock evtProperties = AudioPropertyBlock.Default;

            if (clip == null && (cmd.Flags & AudioPlaybackFlags.SecondaryClipOverride) != 0) {
                clip = Find.FromId(cmd.SecondaryAsset.InstanceId) as AudioClip;
            }
            
            if (evt != null) {
                if (clip == null) {
                    if (evt.SampleSelector == null) {
                        evt.SampleSelector = new RandomDeck<AudioClip>(evt.Samples);
                    }
                    clip = evt.SampleSelector.Next();
                }

                evtProperties.Volume = evt.Volume.Generate();
                evtProperties.Pitch = evt.Pitch.Generate();
                delay = evt.Delay.Generate();

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

            if (clip == null) {
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

#if UNITY_EDITOR
                voiceComponents.gameObject.name = clip.name;
#endif // UNITY_EDITOR
            }

            src.clip = clip;
            src.priority = priority;
            src.loop = (cmd.Flags & AudioPlaybackFlags.Loop) != 0;

            if (clip.loadState == AudioDataLoadState.Unloaded) {
                if (delay > 0) {
                    m_PreloadQueue.PushBack(clip);
                } else {
                    m_PreloadQueue.PushFront(clip);
                }
            }

            AudioEmitterConfig.ApplyConfiguration(src, emitterConfig, m_HasSpatializationPlugin);
            voiceComponents.gameObject.SetActive(true);
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

            voice.EventId = evt ? evt.CachedId : default;

            if ((cmd.Flags & AudioPlaybackFlags.UseProvidedSource) == 0) {
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
    }
}