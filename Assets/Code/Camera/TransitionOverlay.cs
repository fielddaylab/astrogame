using System.Collections;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
using FieldDay.UI.Animation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astro {
    public sealed class TransitionOverlay : SharedStateComponent {
        public GuiFader DefaultFader;

        private void Awake() {
            Game.Scenes.RegisterTransitionHandlers(UnloadHandler, LoadHandler);
            if (!GameLoop.IsBooted()) {
                DefaultFader.Show(Color.black, 0);
            } else {
                DefaultFader.Hide(0, false);
            }

            Game.Scenes.OnMainSceneUnloading.Register(OnMainSceneUnloading);
            Game.Scenes.OnMainSceneReady.Register(OnMainSceneReady);

            //Game.Gui.FlushCommands();
        }

        private void OnMainSceneUnloading() {
            Game.Input.PauseDevices();
            Game.Input.PauseRaycasts();
        }

        private void OnMainSceneReady() {
            Game.Input.ResumeDevices();
            Game.Input.ResumeRaycasts();
        }

        private IEnumerator UnloadHandler(Scene scene, StringHash32 tag) {
            DefaultFader.Show(Color.black, 0.5f);
            yield return 0.55f;
        }

        private IEnumerator LoadHandler(Scene scene, StringHash32 tag) {
            DefaultFader.Hide(0.5f, false);
            yield return 0.2f;
        }
    }
}