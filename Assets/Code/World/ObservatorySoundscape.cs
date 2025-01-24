using System;
using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using UnityEngine;

namespace Astro.Audio {
    public sealed class ObservatorySoundscape : MonoBehaviour {

        [AudioEventRef] public StringHash32 BaseHum;

        [Header("Oneshot")]
        [AudioEventRef] public StringHash32[] RandomizedOneshots;
        public float OneshotDelay;
        public float OneshotDelayRandom;

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
                var sfx = Sfx.Play(RNG.Instance.Choose(RandomizedOneshots));
                Sfx.OverrideTag(sfx, "soundscape");
                delay = (OneshotDelay + RNG.Instance.NextFloat(OneshotDelayRandom));
            }
        }
    }
}