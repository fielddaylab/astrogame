using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public sealed class PrefetchPreloader : MonoBehaviour, IScenePreload {
        public TextAsset Manifest;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            if (Manifest) {
                return AstroPrefetch.ManifestAsync(Manifest);
            }
            return null;
        }
    }
}