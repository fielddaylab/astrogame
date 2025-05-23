using System;
using EasyAssetStreaming;
using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    public enum DocumentCategory {
        Flavor,
        Reference,
        Story,
        Important
    }

    [System.Serializable]
    public struct StreamingDocumentVisual {
        [StreamingPath][SerializeField]
        public string VisualAssetPath;
        [StreamingPath][SerializeField]
        public string LowResAssetPath;
    }

    [CreateAssetMenu(menuName = "AstroGame/Document Asset")]
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;
        [HideInInspector] public DocumentInteractable Interactable = null;

        [TextArea(1, 64)]
        [SerializeField] public string[] TextFields;

        [HideInInspector]
        public StreamingDocumentVisual[] StreamingVisuals;

        public Vector3 DefaultPinnedPos;
        public Vector3 ZoomOffsetOverride;

        public bool PreserveInArchive = true;
        public bool TriggersPrompter = true; // false for questions

#if UNITY_EDITOR
        private void OnEnable() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Prefab == null) return;

            int numStreamingAssets = 0;
            foreach (DocumentRenderComponent cmp in Prefab.RenderComponents) {
                numStreamingAssets += cmp.NumStreamingVisuals;
            }

            if (TextFields.Length != Prefab.TextRegions.Length) {
                Array.Resize(ref TextFields, Prefab.TextRegions.Length);
            }
            if (StreamingVisuals.Length != numStreamingAssets) {
                Array.Resize(ref StreamingVisuals, numStreamingAssets);
            }

            if (Prefab.gameObject.TryGetComponent(out DocumentInteractable interactable)) {
                Interactable = interactable;
            }
        }

        private void OnValidate() {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (Prefab == null) return;
            int numStreamingAssets = 0;
            foreach (DocumentRenderComponent cmp in Prefab.RenderComponents)
            {
                numStreamingAssets += cmp.NumStreamingVisuals;
            }
            Array.Resize(ref TextFields, Prefab.TextRegions.Length);
            Array.Resize(ref StreamingVisuals, numStreamingAssets);

            if (Prefab.gameObject.TryGetComponent(out DocumentInteractable interactable)) {
                Interactable = interactable;
            }
        }
#endif
    }
}