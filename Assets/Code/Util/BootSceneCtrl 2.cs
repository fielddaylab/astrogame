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
        public Transform Plane;
        public Transform CameraToRotate;
        public AudioSource AudioSource;
        public SceneReference NextScene;
        public FadeGroup LoadingGroup;
        public FadeGroup PromptGroup;
        public GuiFader FadeOut;

        private void Awake() {
            Plane.localEulerAngles = new Vector3(RNG.Instance.NextFloat(360), RNG.Instance.NextFloat(360), RNG.Instance.NextFloat(360));
            FadeOut.Hide(0);

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
            FadeOut.Show(Color.black, 1);
            yield return 1;
            Game.Scenes.LoadMainScene(NextScene);
        }

        private void LateUpdate() {
            CameraToRotate.Rotate(0, -3 * Time.deltaTime, 0, Space.Self);
        }
    }
}