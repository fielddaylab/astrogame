using BeauUtil;
using FieldDay.Components;
using System.Collections.Generic;
using UnityEngine;

namespace FieldDay.Scenes {
    public abstract class SceneController : MonoBehaviour, IScenePreload {
        /// <summary>
        /// Invoked when the scene is preloading.
        /// </summary>
        protected virtual IEnumerator<WorkSlicer.Result?> OnScenePreload() {
            return null;
        }

        /// <summary>
        /// Invoked when all preloading and late-activation is done.
        /// </summary>
        protected virtual void OnSceneEnable() {

        }

        /// <summary>
        /// Invoked when the scene is marked as ready.
        /// </summary>
        protected virtual void OnSceneReady() {

        }

        /// <summary>
        /// Invoked when the scene unloads.
        /// </summary>
        protected virtual void OnSceneUnload() {

        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            Game.Scenes.QueueOnEnable(this, OnSceneEnable);
            Game.Scenes.QueueOnLoad(this, OnSceneReady);
            Game.Scenes.QueueOnUnload(this, OnSceneUnload);
            return OnScenePreload();
        }
    }
}