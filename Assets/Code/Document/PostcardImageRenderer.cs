using EasyAssetStreaming;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(StreamingQuadTexture))]
    public sealed class PostcardImageRenderer : DocumentRenderComponent {
        public override int NumStreamingVisuals => 1;
        [HideInInspector] public StreamingQuadTexture PostcardImage;

        private void Awake() {
            PostcardImage = GetComponent<StreamingQuadTexture>();
        }

        public override void SetFullDisplay(StreamingDocumentVisual asset) {
            PostcardImage.Path = default;

            PostcardImage.Path = asset.VisualAssetPath;
            PostcardImage.Preload();
            PostcardImage.enabled = true;
        }

        public override void SetLowResDisplay(StreamingDocumentVisual asset) {
            PostcardImage.enabled = false;
            PostcardImage.Path = asset.LowResAssetPath;
            PostcardImage.Preload();
            PostcardImage.enabled = true;
        }

        public override void Hide() {
            PostcardImage.enabled = false;
        }
    }
}