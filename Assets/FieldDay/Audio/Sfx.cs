using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    static public class Sfx {

        #region Play

        static public AudioHandle Play(StringHash32 eventId) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                });
        }

        static public AudioHandle Play(StringHash32 eventId, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    RotationOffset = Quaternion.identity,
                });
        }

        static public AudioHandle Play(StringHash32 eventId, Transform position) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(position),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                });
        }

        static public AudioHandle Play(StringHash32 eventId, Transform position, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(position),
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    RotationOffset = Quaternion.identity,
                });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Transform position) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    TransformOffset = position.position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = position.rotation,
                });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Transform position, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    TransformOffset = position.position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = position.rotation,
                });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Vector3 position, Quaternion rotation) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    TransformOffset = position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = rotation,
                });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Vector3 position, Quaternion rotation, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    TransformOffset = position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = rotation,
                });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioSource source) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource
                });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioSource source, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource
                });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioClip clipOverride, AudioSource source) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    SecondaryAsset = clipOverride,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource | AudioPlaybackFlags.SecondaryClipOverride
                });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioClip clipOverride, AudioSource source, SfxPlayArgs playArgs) {
            return Game.Audio.QueuePlayAudioCommand(AudioCommandType.PlayClipFromName,
                new PlayCommandData() {
                    Asset = eventId,
                    SecondaryAsset = clipOverride,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = playArgs.Volume,
                    Pitch = playArgs.Pitch,
                    Delay = playArgs.Delay,
                    Pan = playArgs.Pan,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource | AudioPlaybackFlags.SecondaryClipOverride
                });
        }

        #endregion // Play

        #region Stop

        static public void Stop(AudioHandle handle) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithHandle,
                Stop = new StopCommandData() {
                    Id = new AudioIdRef() {
                        Handle = handle.m_Id
                    }
                }
            });
        }

        static public void Stop(AudioHandle handle, float fadeDuration) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithHandle,
                Stop = new StopCommandData() {
                    Id = new AudioIdRef() {
                        Handle = handle.m_Id
                    },
                    FadeOut = fadeDuration,
                    FadeOutCurve = Curve.Linear
                }
            });
        }

        static public void Stop(AudioSource source) {
            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithAudioSource,
                Stop = new StopCommandData() {
                    Id = source,
                }
            });
        }

        static public void Stop(AudioSource source, float fadeDuration) {
            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithAudioSource,
                Stop = new StopCommandData() {
                    Id = source,
                    FadeOut = fadeDuration,
                    FadeOutCurve = Curve.Linear
                }
            });
        }

        static public void StopAllWithTag(StringHash32 tag) {
            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithTag,
                Stop = new StopCommandData() {
                    Id = new AudioIdRef() {
                        Id = tag
                    }
                }
            });
        }

        static public void StopAllWithTag(StringHash32 tag, float fadeDuration) {
            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithTag,
                Stop = new StopCommandData() {
                    Id = new AudioIdRef() {
                        Id = tag
                    },
                    FadeOut = fadeDuration
                }
            });
        }

        static public void StopAll() {
            Game.Audio?.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopAll
            });
        }

        #endregion // Stop

        #region Queries

        static public bool WasAudible(AudioHandle handle) {
            return Game.Audio.WasVoiceAudible(handle);
        }

        static public bool IsActive(AudioHandle handle) {
            return Game.Audio.IsVoiceActive(handle);
        }

        static public AudioSource GetSource(AudioHandle handle) {
            return Game.Audio.GetVoiceSource(handle);
        }

        #endregion // Queries

        #region Properties

        static public void OverrideTag(AudioHandle handle, StringHash32 tag) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetTagWithHandle,
                SetTag = new OverwriteTagCommandData() {
                    Handle = handle.m_Id,
                    Tag = tag
                }
            });
        }

        static public void SetVolume(AudioHandle handle, float volume, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetVoiceFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = handle.m_Id,
                    Property = AudioFloatPropertyType.Volume,
                    Target = volume,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetPitch(AudioHandle handle, float pitch, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetVoiceFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = handle.m_Id,
                    Property = AudioFloatPropertyType.Pitch,
                    Target = pitch,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetPan(AudioHandle handle, float pan, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetVoiceFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = handle.m_Id,
                    Property = AudioFloatPropertyType.Pan,
                    Target = pan,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetPaused(AudioHandle handle, bool paused) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetVoiceBoolParameter,
                BoolParam = new BoolParamChangeCommandData() {
                    Handle = handle.m_Id,
                    Property = AudioBoolPropertyType.Pause,
                    Target = paused
                }
            });
        }

        static public void SetMute(AudioHandle handle, bool mute) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetVoiceBoolParameter,
                BoolParam = new BoolParamChangeCommandData() {
                    Handle = handle.m_Id,
                    Property = AudioBoolPropertyType.Mute,
                    Target = mute
                }
            });
        }

        static public void SetBusPaused(StringHash32 busId, bool paused) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetBusBoolParameter,
                BoolParam = new BoolParamChangeCommandData() {
                    Handle = busId,
                    Property = AudioBoolPropertyType.Pause,
                    Target = paused
                }
            });
        }

        static public void SetBusVolume(StringHash32 busId, float volume, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetBusFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = busId,
                    Property = AudioFloatPropertyType.Volume,
                    Target = volume,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetBusPitch(StringHash32 busId, float pitch, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetBusFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = busId,
                    Property = AudioFloatPropertyType.Pitch,
                    Target = pitch,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetBusLoPass(StringHash32 busId, float loPass, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetBusFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = busId,
                    Property = AudioFloatPropertyType.LoPass,
                    Target = loPass,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void SetBusHiPass(StringHash32 busId, float hiPass, float transitionTime = 0, Curve transitionCurve = Curve.Linear) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.SetBusFloatParameter,
                FloatParam = new FloatParamChangeCommandData() {
                    Handle = busId,
                    Property = AudioFloatPropertyType.HiPass,
                    Target = hiPass,
                    Duration = transitionTime,
                    Easing = transitionCurve,
                }
            });
        }

        static public void Seek(AudioHandle handle, float position) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.Seek,
                Seek = new SeekCommandData() {
                    Handle = handle.m_Id,
                    Position = position
                }
            });
        }

        #endregion // Properties
    }

    public struct SfxPlayArgs {
        public float Volume;
        public float Pitch;
        public float Delay;
        public float Pan;
    }
}