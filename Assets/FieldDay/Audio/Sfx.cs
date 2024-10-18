using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    static public class Sfx {

        #region Play

        static public AudioHandle Play(StringHash32 eventId) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                }
            });
        }

        static public AudioHandle Play(StringHash32 eventId, Transform position) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(position),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                }
            });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Transform position) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    TransformOffset = position.position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = position.rotation,
                }
            });
        }

        static public AudioHandle PlayDetached(StringHash32 eventId, Vector3 position, Quaternion rotation) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    Volume = 1,
                    Pitch = 1,
                    TransformOffset = position,
                    TransformOffsetSpace = Space.World,
                    RotationOffset = rotation,
                }
            });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioSource source) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource
                }
            });
        }

        static public AudioHandle PlayFrom(StringHash32 eventId, AudioClip clipOverride, AudioSource source) {
            return Game.Audio.QueuePlayAudioCommand(new AudioCommand() {
                Type = AudioCommandType.PlayClipFromName,
                Play = new PlayCommandData() {
                    Asset = eventId,
                    SecondaryAsset = clipOverride,
                    TransformOrAudioSourceId = UnityHelper.Id(source),
                    Volume = 1,
                    Pitch = 1,
                    RotationOffset = Quaternion.identity,
                    Flags = AudioPlaybackFlags.UseProvidedSource | AudioPlaybackFlags.SecondaryClipOverride
                }
            });
        }

        #endregion // Play

        #region Stop

        static public void Stop(AudioHandle handle) {
            if (!handle.IsValid) {
                return;
            }

            Game.Audio.QueueAudioCommand(new AudioCommand() {
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

            Game.Audio.QueueAudioCommand(new AudioCommand() {
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
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithAudioSource,
                Stop = new StopCommandData() {
                    Id = source,
                }
            });
        }

        static public void Stop(AudioSource source, float fadeDuration) {
            Game.Audio.QueueAudioCommand(new AudioCommand() {
                Type = AudioCommandType.StopWithAudioSource,
                Stop = new StopCommandData() {
                    Id = source,
                    FadeOut = fadeDuration,
                    FadeOutCurve = Curve.Linear
                }
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

        #endregion // Queries
    }
}