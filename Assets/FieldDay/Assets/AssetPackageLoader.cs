using System.Collections.Generic;
using BeauUtil;
using FieldDay.Scenes;
using UnityEngine;

namespace FieldDay.Assets {
    /// <summary>
    /// Preloads a set of asset packages.
    /// </summary>
    public sealed class AssetPackageLoader : MonoBehaviour, IScenePreload {
        [SerializeField] private AssetPack[] m_Packs;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            foreach(var pack in m_Packs) {
                Game.Assets.LoadPackage(pack);
                yield return null;
            }

            Game.Scenes.QueueOnUnload(this, OnSceneUnloading);
        }

        private void OnSceneUnloading() {
            foreach (var pack in m_Packs) {
                Game.Assets.UnloadPackage(pack);
            }
        }
    }
}