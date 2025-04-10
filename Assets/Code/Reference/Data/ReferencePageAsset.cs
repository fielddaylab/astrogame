
using EasyAssetStreaming;
using FieldDay.Assets;
using UnityEngine;

namespace Astro {

    [CreateAssetMenu(menuName = "AstroGame/Reference/Page")]
    public class ReferencePageAsset : NamedAsset {
        [Header("Revised")]
        [StreamingImagePath] public string BackgroundImagePath;
        public ReferenceClassification[] Classifications;
    }
}