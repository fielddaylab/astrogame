using System;
using TMPro;
using UnityEngine;
using EasyAssetStreaming;
using FieldDay.Components;

namespace Astro {
    [RequireComponent(typeof(DocumentInteractable))]
    public sealed class DocumentRenderer : BatchedComponent {
        [Tooltip("A collection of TMP_Text areas for display which corrisponds to TextFields in DocumentAsset.")]
        public TextRegion[] TextRegions;
        public GameObject LowResTextPrefab;

        public DocumentRenderComponent[] RenderComponents;
        [NonSerialized] public StreamingQuadTexture[] StreamingTextures;

        public Rect Size;
        [SerializeField] private bool m_ShowSizeRect = false;

        public Vector3 ZoomOffsetOverride;

        [HideInInspector] public DocumentInteractable Interactable;
        [HideInInspector] public bool TriggersPrompter;

        [NonSerialized] public string BaseVisualAssetName;
        [NonSerialized] public string BaseVisualAssetFileType;

        public bool AlwaysHighRes = false;
        public DocumentThickness Thickness = DocumentThickness.Thick;

        private void Awake() {
            Interactable = GetComponent<DocumentInteractable>();

            StreamingTextures = GetComponentsInChildren<StreamingQuadTexture>();
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
                    if (renderer.TextRegions[i].Text.text.Length > 0) {
                        GameObject.Destroy(tmpText.transform.GetChild(j).gameObject);
                    }
                }

                tmpText.SetText(asset.TextFields[i]);
                if (asset.Font) { tmpText.font = asset.Font; }
                tmpText.lineSpacing = asset.LineSpacing != 0 ? asset.LineSpacing : tmpText.lineSpacing;
                tmpText.fontStyle = asset.FontStyle;
                tmpText.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }

            for (int i = 0; i < renderer.RenderComponents.Length; i++) {
                DocumentRenderComponent component = renderer.RenderComponents[i];
                component.SetFullDisplay(asset.StreamingVisuals[i]);
            }

            if (renderer.ZoomOffsetOverride == default) {
                renderer.ZoomOffsetOverride = asset.ZoomOffsetOverride;
            }
            else if (asset.ZoomOffsetOverride != default) {
                renderer.ZoomOffsetOverride = asset.ZoomOffsetOverride;
            }

            AstroGame.Events.Dispatch(GameEvents.ActiveDocChanged, asset.name);
        }

        public static void DisplayLowResDocument(DocumentRenderer renderer, DocumentAsset asset) {
            for (int i = 0; i < renderer.TextRegions.Length; i++) {
                if (renderer.TextRegions[i].LowResSprite == null || renderer.TextRegions[i].Text.text.Length <= 0) continue;
                
                GameObject lowResImage = GameObject.Instantiate(renderer.LowResTextPrefab, renderer.TextRegions[i].Text.transform);
                SpriteRenderer lowResSpriteRender = lowResImage.GetComponent<SpriteRenderer>();
                lowResSpriteRender.sprite = asset.LowResTextFields[i];
                lowResSpriteRender.size = ((RectTransform) lowResImage.transform.parent).sizeDelta;

                renderer.TextRegions[i].Text.gameObject.GetComponent<MeshRenderer>().enabled = false;
                lowResImage.GetComponent<SpriteRenderer>().enabled = true;
            }

            for (int i = 0; i < renderer.RenderComponents.Length; i++) {
                DocumentRenderComponent component = renderer.RenderComponents[i];
                component.SetLowResDisplay(asset.StreamingVisuals[i]);
            }
        }

        public static bool IsFullyLoaded(DocumentRenderer renderer)
        {
            foreach (var tex in renderer.StreamingTextures)
            {
                if (tex.IsLoading()) { return false; }
            }

            return true;
        }
    }

	public enum DocumentThickness : byte {
        Thin,
        Thick
    }
}