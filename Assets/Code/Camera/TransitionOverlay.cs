using System;
using System.Collections;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.UI;
using FieldDay.UI.Animation;
using FieldDay.Vox;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Astro {
    public sealed class TransitionOverlay : SharedStateComponent {
        static public readonly StringHash32 Type_CopyTexture = "CopyTexture";

        public GuiFader DefaultFader;
        //public RawImage TextureOverlay;

        [NonSerialized] public Texture2D TextureCopy;

        private void Awake() {
            Game.Scenes.RegisterTransitionHandlers(UnloadHandler, LoadHandler);
            
            if (!GameLoop.IsBooted() && SceneManager.GetActiveScene().buildIndex != 0) {
                DefaultFader.Show(Color.black, 0);
                Sfx.SetMixState("SceneTransition_FadeOut", 1);
            } else {
                DefaultFader.Hide(0, false);
            }

            Game.Scenes.OnMainSceneUnloading.Register(OnMainSceneUnloading);
            Game.Scenes.OnMainSceneReady.Register(OnMainSceneReady);
        }

        private void OnMainSceneUnloading() {
            Game.Input.PauseDevices();
            Game.Input.PauseRaycasts();
        }

        private void OnMainSceneReady() {
            Game.Input.ResumeDevices();
            Game.Input.ResumeRaycasts();
        }

        private IEnumerator UnloadHandler(Scene scene, StringHash32 tag, MainSceneTransitionArgs transition) {
            if (transition.ShouldSkip) {
                yield break;
            }

            if (transition.TransitionType == Type_CopyTexture) {
                //TextureOverlay.enabled = true;
            } else {
                DefaultFader.Show(Color.black, 0.5f);
                Sfx.SetMixState("SceneTransition_FadeOut", 1, 0.55f);
                yield return 0.55f;
                VoxUtility.UnloadAll();
            }
        }

        private IEnumerator LoadHandler(Scene scene, StringHash32 tag, MainSceneTransitionArgs transition) {
            if (transition.ShouldSkip) {
                yield break;
            }

            if (transition.TransitionType == Type_CopyTexture) {
                //TextureOverlay.enabled = false;
                //TextureOverlay.texture = null;
            } else {
                DefaultFader.Hide(0.5f, 0.04f, false);
                Sfx.SetMixState("SceneTransition_FadeOut", 0, 0.4f);
                yield return 0.2f;
            }
        }
    }
}