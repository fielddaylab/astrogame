using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Document Asset")]
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;
        [SerializeField] public string TitleText;
        [TextArea] 
        [SerializeField] public string BodyText;
        public Vector3 ZoomOffsetOverride;
    }

    public enum DocumentCategory {
        Flavor,
        Reference,
        Story,
        Important
    }
}