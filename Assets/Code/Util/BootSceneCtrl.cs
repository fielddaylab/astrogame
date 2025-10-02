using System.Collections;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.UI.Animation;
using NativeUtils;
using UnityEngine;

namespace Astro {
    public sealed class BootSceneCtrl : SceneController {
        public AudioSource AudioSource;
        public SceneReference NextScene;
        public FadeGroup LoadingGroup;
        public FadeGroup PromptGroup;
        public UnityEngine.Object[] UnloadAssets;

        protected override void OnSceneReady() {
            NativeInput.OnMouseDown += OnNativeClick;
            LoadingGroup.Hide();
            PromptGroup.Show();
        }

        protected override void OnSceneUnload() {
            foreach(var obj in UnloadAssets) {
                AssetUtility.DestroyAsset(obj);
            }
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