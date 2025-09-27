using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Scenes;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed class SfxLoop : BatchedComponent, IRegistrationCallbacks {
        [AudioEvent] public StringHash32 EventId;
        public float StopFadeDuration = 0;

        [NonSerialized] public AudioHandle Handle;

        void IRegistrationCallbacks.OnDeregister() {
            Sfx.Stop(Handle, StopFadeDuration);
            Handle = default;
        }

        void IRegistrationCallbacks.OnRegister() {
            if (EventId.IsEmpty) {
                return;
            }

            if (Game.Scenes.IsLoaded(gameObject.scene)) {
                Play();
            } else {
                Game.Scenes.QueueOnLoad(Play);
            }
        }

        private void Play() {
            Handle = Sfx.Play(EventId, transform);
        }
    }
}