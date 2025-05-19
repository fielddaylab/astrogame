using System;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct TextRegion {
        public TMP_Text Text;
        public Sprite LowResSprite;
    }

    [RequireComponent(typeof(DocumentInteractable))]
    public sealed class DocumentRenderer : BatchedComponent {
        [Tooltip("A collection of TMP_Text areas for display which corrisponds to TextFields in DocumentAsset.")]
        public TextRegion[] TextRegions;
        public GameObject LowResText;

        public DocumentRenderComponent[] RenderComponents;

        public Rect Size;
        [SerializeField] private bool m_ShowSizeRect = false;

        public Vector3 ZoomOffsetOverride;

        [HideInInspector] public DocumentInteractable Interactable;

        [NonSerialized] public string BaseVisualAssetName;
        [NonSerialized] public string BaseVisualAssetFileType;

        private void Awake() {
            Interactable = GetComponent<DocumentInteractable>();
        }

        void OnDrawGizmos() {
            if (!m_ShowSizeRect) return; 

            Gizmos.color = new Color(0, 1, 1, 0.25f);
            Gizmos.DrawCube(new Vector3(Size.x, Size.y, 0f), new Vector3(Size.width, Size.height, 0)); 
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
                    if (renderer.TextRegions[j].Text.text.Length > 0) {
                        GameObject.Destroy(tmpText.transform.GetChild(j).gameObject);
                    }
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
        }

        public static void DisplayLowResDocument(DocumentRenderer renderer, DocumentAsset asset) {
            for (int i = 0; i < renderer.TextRegions.Length; i++) {
                if (renderer.TextRegions[i].LowResSprite != null && renderer.TextRegions[i].Text.text.Length > 0) {
                    GameObject lowResImage = GameObject.Instantiate(renderer.LowResText, renderer.TextRegions[i].Text.transform);
                    SpriteRenderer lowResSpriteRender = lowResImage.GetComponent<SpriteRenderer>();
                    lowResSpriteRender.sprite = renderer.TextRegions[i].LowResSprite;
                    lowResSpriteRender.size = ((RectTransform) lowResImage.transform.parent).sizeDelta;

                    renderer.TextRegions[i].Text.gameObject.GetComponent<MeshRenderer>().enabled = false;
                    lowResImage.GetComponent<SpriteRenderer>().enabled = true;
                }
            }

            for (int i = 0; i < renderer.RenderComponents.Length; i++) {
                DocumentRenderComponent component = renderer.RenderComponents[i];
                component.SetLowResDisplay(asset.StreamingVisuals[i]);
            }
        }
    }
}