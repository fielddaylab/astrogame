using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;
    }

    public enum DocumentCategory {
        Flavor,
        Reference,
        Story,
        Important
    }
}