using UnityEngine;

namespace FieldDay.Audio {
    [RequireComponent(typeof(AudioListener))]
    public sealed class AudioListenerReference : MonoBehaviour {
        private void OnEnable() {
            Game.Audio.SetListener(GetComponent<AudioListener>());
        }

        private void OnDisable() {
            Game.Audio?.RemoveListener(GetComponent<AudioListener>());
        }
    }
}