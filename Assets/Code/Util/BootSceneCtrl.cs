using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.UI.Animation;
using NativeUtils;
using UnityEngine;

namespace Astro {
    public sealed class BootSceneCtrl : MonoBehaviour {
        public AudioSource AudioSource;
        public SceneReference NextScene;
        public FadeGroup LoadingGroup;
        public FadeGroup PromptGroup;
        
        private void Awake() {
            Game.Scenes.QueueOnLoad(() => {
                NativeInput.OnMouseDown += OnNativeClick;
                LoadingGroup.Hide();
                PromptGroup.Show();
            });
        }

        private void OnNativeClick(float normX, float normY) {
            NativeInput.OnMouseDown -= OnNativeClick;
            AudioUtility.WakeUpNativeAudio();
            AudioSource.Play();
            Routine.Start(this, FinishedSequence());
        }

        private IEnumerator FinishedSequence() {
            yield return 0.2f;
            Game.Scenes.LoadMainScene(NextScene);
        }
    }
}