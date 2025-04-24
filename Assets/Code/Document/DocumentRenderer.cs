using System;
using BeauUtil;
using EasyAssetStreaming;
using FieldDay.Components;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct TextRegion {
        public TMP_Text Text;
        [StreamingImagePath] public string LowResImage;
    }

    [RequireComponent(typeof(DocumentInteractable))]
    public sealed class DocumentRenderer : BatchedComponent {
        [Tooltip("A collection of TMP_Text areas for display which corrisponds to TextFields in DocumentAsset.")]
        public TextRegion[] TextRegions;
        public GameObject LowResText;

        public DocumentRenderComponent[] RenderComponents;

        public Rect Size;
        public Vector3 ZoomOffsetOverride;

        [HideInInspector] public DocumentInteractable Interactable;

        [NonSerialized] public string BaseVisualAssetName;
        [NonSerialized] public string BaseVisualAssetFileType;

        private void Awake() {
            Interactable = GetComponent<DocumentInteractable>();
            // RenderComponents = GetComponentsInChildren<DocumentRenderComponent>(true);
        }
    }


    public abstract class DocumentRenderComponent : BatchedComponent {
        public abstract int NumStreamingVisuals { get; }

        public abstract void SetFullDisplay(StreamingDocumentVisual asset);
        public abstract void SetLowResDisplay(StreamingDocumentVisual asset);
        public abstract void Hide();
    }

    public static partial class DocumentUtility {
        public static void DisplayFullDocument(DocumentRenderer renderer, DocumentAsset asset) {
            for(int i = 0; i < renderer.TextRegions.Length; i++) {
                TMP_Text tmpText = renderer.TextRegions[i].Text;
                // Remove any low res images
                for (int j = 0; j < tmpText.transform.childCount; j++) {
                    GameObject.Destroy(tmpText.transform.GetChild(j).gameObject); 
                }

                tmpText.SetText(asset.TextFields[i]);
                tmpText.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }

            for (int i = 0; i < renderer.RenderComponents.Length; i++) {
                DocumentRenderComponent component = renderer.RenderComponents[i];
                component.SetFullDisplay(asset.StreamingVisuals[i]);
            }

            renderer.transform.localPosition = asset.DefaultPinnedPos;
            if (renderer.ZoomOffsetOverride == default) {
                renderer.ZoomOffsetOverride = asset.ZoomOffsetOverride;
            }
            else if (asset.ZoomOffsetOverride != default) {
                renderer.ZoomOffsetOverride = asset.ZoomOffsetOverride;
            }

            // renderer.Interactable.Renderer = renderer;
            // renderer.Interactable.Parts = renderer.Interactable.GetComponentsInChildren<DocumentPart>(true);
        }

        public static void DisplayLowResDocument(DocumentRenderer renderer, DocumentAsset asset) {
            for (int i = 0; i < renderer.TextRegions.Length; i++) {
                if (renderer.TextRegions[i].LowResImage.Length > 0) {
                    GameObject lowResImage = GameObject.Instantiate(renderer.LowResText, renderer.TextRegions[i].Text.transform);
                    lowResImage.GetComponent<StreamingQuadTexture>().Path = renderer.TextRegions[i].LowResImage;
                    lowResImage.GetComponent<StreamingQuadTexture>().Preload();

                    // TODO Update this 
                    renderer.TextRegions[i].Text.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    lowResImage.GetComponent<StreamingQuadTexture>().enabled = true;
                }
            }

            for (int i = 0; i < renderer.RenderComponents.Length; i++) {
                DocumentRenderComponent component = renderer.RenderComponents[i];
                component.SetLowResDisplay(asset.StreamingVisuals[i]);
            }
        }
    }
}