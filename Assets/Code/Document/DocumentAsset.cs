using System;
using EasyAssetStreaming;
using FieldDay;
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
        // TODO Update later when we have an atlas for this 
        [StreamingPath][SerializeField]
        public string LowResAssetPath;
    }

    [CreateAssetMenu(menuName = "AstroGame/Document Asset")]
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;

        [TextArea] 
        [SerializeField] public string[] TextFields;

        // public StreamingDocumentVisual DocumentVisualTest;
        [HideInInspector]
        public StreamingDocumentVisual[] StreamingVisuals;

        public Vector3 DefaultPinnedPos;
        public Vector3 ZoomOffsetOverride;

#if UNITY_EDITOR
        private void OnValidate() { 
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return; 

            if (Prefab == null) return;
            int numStreamingAssets = 0;
            foreach(DocumentRenderComponent cmp in Prefab.RenderComponents){
                numStreamingAssets += cmp.NumStreamingVisuals;
            }
            Array.Resize(ref TextFields, Prefab.TextRegions.Length);
            Array.Resize(ref StreamingVisuals, numStreamingAssets);
        }
#endif
    }
}