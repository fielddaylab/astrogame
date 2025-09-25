using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using UnityEngine;

namespace Astro.Audio {
    public sealed class ObservatorySoundscape : MonoBehaviour {

        [AudioEvent] public StringHash32 BaseHum;

        [Header("Oneshot")]
        [AudioEvent] public StringHash32[] RandomizedOneshots;
        public float OneshotDelay;
        public float OneshotDelayRandom;
        public float OneshotPlaybackRadius = 20;

        [NonSerialized] private AudioHandle m_HumHandle;

        private void Awake() {
            Game.Scenes.QueueOnLoad(() => {
                Routine.Start(this, Playback());
            });
        }

        private void OnDestroy() {
            Sfx.Stop(m_HumHandle);
            Sfx.StopAllWithTag("soundscape");
        }

        private IEnumerator Playback() {
            m_HumHandle = Sfx.Play(BaseHum);

            float delay = 0.5f * (OneshotDelay + RNG.Instance.NextFloat(OneshotDelayRandom));
            while(true) {
                yield return delay;

                Vector3 pos = transform.position;
                Vector2 offset = RNG.Instance.NextVector2(OneshotPlaybackRadius, OneshotPlaybackRadius);
                pos.x += offset.x;
                pos.z += offset.y;
                
                var sfx = Sfx.PlayDetached(RNG.Instance.Choose(RandomizedOneshots), pos, Quaternion.identity);
                Sfx.OverrideTag(sfx, "soundscape");
                delay = (OneshotDelay + RNG.Instance.NextFloat(OneshotDelayRandom));
            }
        }
    }
}