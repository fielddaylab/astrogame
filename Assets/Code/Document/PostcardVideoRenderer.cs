using System;
using UnityEngine;
using UnityEngine.Video;

namespace Astro {
    public sealed class PostcardVideoRenderer : DocumentRenderComponent {
        public VideoPlayer Video;
        public Transform FrontAnimation;

        public override int NumStreamingVisuals => 1;

        public override void Hide() {
        }

        public override void SetFullDisplay(StreamingDocumentVisual asset) {
            throw new NotImplementedException();

            // // Init video
            // if (renderer.Video != null) {
            //     renderer.Video.gameObject.SetActive(true);

            //     renderer.Video.url = default;

            //     if (asset.VideoName.Length == 0) {
            //         renderer.FrontAnimation.gameObject.SetActive(false);
            //     }
            //     else {
            //         renderer.BaseVisualAssetName = asset.VideoName;
            //         renderer.BaseVisualAssetFileType = "." + asset.FileType;
            //         renderer.Video.url = Application.streamingAssetsPath + POSTCARD_DIR + renderer.BaseVisualAssetName + renderer.BaseVisualAssetFileType;
            //         renderer.FrontAnimation.gameObject.SetActive(true);
            //         renderer.Video.Play();

            //         renderer.LowResImgFront.Path = Application.streamingAssetsPath + POSTCARD_DIR + LOW_RES_ID + renderer.BaseVisualAssetName + DEFAULT_LOW_RES_FILE_TYPE;
            //         renderer.LowResImgFront.Preload();
            //     }
            // }

            // if (doc.Renderer.Video != null){
            //     if (doc.Renderer.Video?.url.Length != 0) {
            //         doc.Renderer.Video.frame = 0;
            //     }
            // }

            // // Replace with high-res assets

            // //  if video, replace with high-res version
            // if (doc.Renderer.Video != null){
            //     if (doc.Renderer.Video?.url?.Length > 0) {
                    // doc.Renderer.FrontAnimation.gameObject.SetActive(true);
                    // doc.Renderer.LowResImgFront.gameObject.SetActive(false);
            //     }
            // }

            //         // play video
            //         if (doc.Renderer.Video) {
            //             if (doc.Renderer.Video.url.Length != 0) {
            //                 doc.Renderer.Video.Play();
            //             }
            //         }
        }

        public override void SetLowResDisplay(StreamingDocumentVisual asset) {
            // // stop video
            // if (doc.Renderer.Video && doc.Renderer.Video.isPlaying) {
            //     doc.Renderer.Video.Pause();
            //     doc.Renderer.Video.frame = (long)doc.Renderer.Video.frameCount - 1;
            // }

            // // Replace with low-res assets
            // if video, replace with low-res version
            // if (doc.Renderer.Video?.url.Length > 0) {
            //     doc.Renderer.FrontAnimation?.gameObject.SetActive(false);
            //     doc.Renderer.LowResImgFront?.gameObject.SetActive(true);
            // }
        }
    }
}