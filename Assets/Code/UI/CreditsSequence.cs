using EasyAssetStreaming;
using UnityEngine;

namespace Astro {
    public sealed class CreditsSequence : MonoBehaviour {
        public StreamingQuadTexture SkyRenderer;
        public StreamingQuadTexture StarsRenderer;
        public Transform StartPosition;
        public Transform FastStartPosition;
        public Transform EndPosition;
    }
}