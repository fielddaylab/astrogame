using UnityEngine;
using UnityEngine.Video;

namespace Astro {
    [RequireComponent(typeof(VideoPlayer))]
    public sealed class PostcardVideoRenderer : DocumentRenderComponent {
        public override int NumStreamingVisuals => 1;
        [HideInInspector] public VideoPlayer Video;

        private void Awake() {
            Video = GetComponent<VideoPlayer>();
            Video.gameObject.SetActive(false);
        }

        public override void Hide() {
            Video.Stop();
            Video.gameObject.SetActive(false);
        }

        public override void SetFullDisplay(StreamingDocumentVisual asset) {
            // Init video
            Video.gameObject.SetActive(true);
            Video.url = default;
            Video.url = Application.streamingAssetsPath + '/' + asset.VisualAssetPath;
            Video.Prepare();

            if (Video.url == null || Video.url.Length <= 0) {
                Debug.LogWarning("[PostcardVideoRenderer] Unable to load video on postcard: " + name);
                return;
            }else{
                Video.prepareCompleted += OnVideoPrepared;
            }
        }

        private void OnVideoPrepared(VideoPlayer src) {
            src.frame = 0;
            src.Play();
        }

        public override void SetLowResDisplay(StreamingDocumentVisual asset) {
            Hide();
        }
    }
}