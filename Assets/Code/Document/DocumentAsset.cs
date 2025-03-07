using FieldDay.Assets;
using UnityEngine;
using UnityEngine.Video;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Document Asset")]
    public sealed class DocumentAsset : NamedAsset {
        public DocumentCategory Category;
        // TODO: Replace with a compressed prefab layout
        public DocumentRenderer Prefab;
        [SerializeField] public string TitleText;
        [TextArea] 
        [SerializeField] public string FrontBodyText;
        [TextArea] 
        [SerializeField] public string BackBodyText;
        public string VideoName;
        public Vector3 DefaultPinnedPos;
        public Vector3 ZoomOffsetOverride;
    }

    public enum DocumentCategory {
        Flavor,
        Reference,
        Story,
        Important
    }
}