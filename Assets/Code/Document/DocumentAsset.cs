using System;
using EasyAssetStreaming;
using FieldDay.Assets;
using TMPro;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct TextRegion {
        public TMP_Text Text;
        public Sprite LowResSprite;
    }

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

    [System.Serializable]
    [CreateAssetMenu(menuName = "AstroGame/Document Asset")]
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;
        [HideInInspector] public DocumentInteractable Interactable = null;

        [TextArea(1, 64)]
        [SerializeField] public string[] TextFields;
        [SerializeField] public Sprite[] LowResTextFields = new Sprite[0];

        [HideInInspector]
        public StreamingDocumentVisual[] StreamingVisuals;
        public bool UseCutoutMaterial = false;

        public Vector3 DefaultPinnedPos;
        [HideInInspector] public Vector3 LastKnownPos;
        public Vector3 ZoomOffsetOverride;

        public bool TriggersPrompter = true; // false for questions

        public bool DifInitPos = false;
        public Vector3 InitPos;

        public TMP_FontAsset Font;
        public float LineSpacing = 0;
        public FontStyles FontStyle = FontStyles.Normal;

        private void OnEnable() {
            SetupDocumentAsset();
        }

#if UNITY_EDITOR
        private void OnValidate() {
            SetupDocumentAsset();
        }
#endif

        /// <summary>
        /// Set the number of `TextFields` and `StreamingAssets` based on the number of Regions in the document's Prefab.
        /// If no prefab is currently defined this function will return immediatley. 
        /// </summary>
        private void SetupDocumentAsset() {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
            if (Prefab == null) return;

            // Calculate the number of streaming assets this document has on its prefab
            int numStreamingAssets = 0;
            foreach (DocumentRenderComponent cmp in Prefab.RenderComponents) {
                numStreamingAssets += cmp.NumStreamingVisuals;
            }

            // Ensure that the number of TextFields on this document matches the number of regions on the prefab
            if (TextFields.Length != Prefab.TextRegions.Length || LowResTextFields.Length != Prefab.TextRegions.Length) {
                Array.Resize(ref TextFields, Prefab.TextRegions.Length);
                Array.Resize(ref LowResTextFields, Prefab.TextRegions.Length);
            }

            // Ensure that the number of StreamingVisuals on this document matches the number of streaming assets on the prefab
            if (StreamingVisuals.Length != numStreamingAssets) {
                Array.Resize(ref StreamingVisuals, numStreamingAssets);
            }

            // If we have an interactable we can hook that up here
            if (Prefab.gameObject.TryGetComponent(out DocumentInteractable interactable)) {
                Interactable = interactable;
            }
        }
    }
}