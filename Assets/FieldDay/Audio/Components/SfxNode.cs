using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed class SfxNode : BatchedComponent, IRegistrationCallbacks {
        [AudioEvent] public StringHash32 EventId;

        [Header("Position")]
        public Transform Position;
        public bool TrackPosition;

        [Header("Config")]
        public float StopFadeDuration = 0;
        public bool AutoPlay;
        public bool AllowDuplicates = false;

        [NonSerialized] public AudioHandle Handle;

        void IRegistrationCallbacks.OnDeregister() {
            Stop();
        }

        void IRegistrationCallbacks.OnRegister() {
            if (EventId.IsEmpty) {
                return;
            }

            if (AutoPlay) {
                if (Game.Scenes.IsLoaded(gameObject.scene)) {
                    Play();
                } else {
                    Game.Scenes.QueueOnLoad(OnSceneLoad);
                }
            }
        }

        private void OnSceneLoad() {
            if (AutoPlay && !Handle.IsValid) {
                Play();
            }
        }

        /// <summary>
        /// Returns if an audio event is playing.
        /// </summary>
        public bool IsPlaying() {
            return Sfx.IsActive(Handle);
        }

        /// <summary>
        /// Starts playback of the audio event.
        /// </summary>
        public void Play() {
            if (!isActiveAndEnabled) {
                return;
            }

            if (!AllowDuplicates && Sfx.IsActive(Handle)) {
                return;
            }

            if (Position) {
                if (TrackPosition) {
                    Handle = Sfx.Play(EventId, Position);
                } else {
                    Handle = Sfx.PlayDetached(EventId, Position);
                }
            } else {
                Handle = Sfx.Play(EventId, transform);
            }
        }

        /// <summary>
        /// Stops playback of the audio event.
        /// </summary>
        public void Stop() {
            Sfx.Stop(Handle, StopFadeDuration);
            Handle = default;
        }
    }
}