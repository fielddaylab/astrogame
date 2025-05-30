using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitlePreludePreloader : MonoBehaviour, IScenePreload {
        public SceneReference PreludeAssetsScene;
        public ViewNode Node;

        [NonSerialized] public bool IsPreloading;

        private void OnNodeLoading() {
            if (IsPreloading) {
                return;
            }

            IsPreloading = true;
            Game.Scenes.LoadAuxScene(PreludeAssetsScene, "prelude");
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Node.OnLoad.Register(OnNodeLoading);
            return null;
        }
    }
}