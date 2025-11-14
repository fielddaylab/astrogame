using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using UnityEngine;

namespace Astro {
    public sealed class DocumentPreloader : SharedStateComponent {
        [NonSerialized] public RingBuffer<StreamingAssetHandle> StreamingHandles = new RingBuffer<StreamingAssetHandle>(16, RingBufferMode.Expand);

        private void Awake() {
            Game.Scenes.QueueOnUnload(ReleaseAllHandles);
        }

        private void ReleaseAllHandles() {
            Log.Msg("[DocumentPreloader] Releasing {0} textures...", StreamingHandles.Count);
            while(StreamingHandles.TryPopFront(out var handle)) {
                Streaming.Unload(handle);
            }
        }

        [LeafMember("PreloadDocument")]
        static public void PreloadDocument(StringHash32 documentId) {
            DocumentAsset documentAsset = Find.NamedAsset<DocumentAsset>(documentId);
            DocumentPreloader preloader = Find.State<DocumentPreloader>();

            Log.Msg("[DocumentPreloader] Preloading document '{0}'", documentId.ToDebugString());

            foreach(var streamingVisual in documentAsset.StreamingVisuals) {
                preloader.PreloadTexture(streamingVisual.VisualAssetPath);
                preloader.PreloadTexture(streamingVisual.LowResAssetPath);
            }
        }

        private void PreloadTexture(string texture) {
            if (string.IsNullOrEmpty(texture)) {
                return;
            }

            if (texture.EndsWith("webm", StringComparison.OrdinalIgnoreCase)) {
                AstroPrefetch.Video(texture);
            } else {
                StreamingHandles.PushBack(Streaming.Texture(texture));
            }
        }
    }
}