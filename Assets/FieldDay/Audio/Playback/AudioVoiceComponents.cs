#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {
    [AddComponentMenu("")]
    internal class AudioVoiceComponents : MonoBehaviour {
        [NonSerialized] public AudioSource Source;

#if SUPPORTS_AUDIOEFFECTS
        [NonSerialized] public AudioLowPassFilter LowPass;
        [NonSerialized] public AudioHighPassFilter HighPass;
#endif // SUPPORTS_AUDIOEFFECTS

        [NonSerialized] public UniqueId16 PlayingHandle;

        private void Awake() {
            hideFlags |= HideFlags.NotEditable;
        }

        private void OnDisable() {
            if (Source) {
                Source.enabled = false;
            }

            PlayingHandle = default;

#if SUPPORTS_AUDIOEFFECTS
            if (LowPass) {
                LowPass.enabled = false;
            }
            if (HighPass) {
                HighPass.enabled = false;
            }
#endif // SUPPORTS_AUDIOEFFECTS
        }

        public void Sync() {
            this.CacheComponent(ref Source);

#if SUPPORTS_AUDIOEFFECTS
            this.CacheComponent(ref LowPass);
            this.CacheComponent(ref HighPass);
#endif // SUPPORTS_AUDIOEFFECTS
        }
    }
}