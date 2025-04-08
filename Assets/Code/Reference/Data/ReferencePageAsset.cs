
using EasyAssetStreaming;
using FieldDay.Assets;
using UnityEngine;

namespace Astro {

    [CreateAssetMenu(menuName = "AstroGame/Reference/Page")]
    public class ReferencePageAsset : NamedAsset {
        [Header("Old")]
        public string TitleLeft;
        public ReferenceClassification[] EntriesLeft;
        public string TitleRight;
        public ReferenceClassification[] EntriesRight;

        [Header("Revised")]
        [StreamingImagePath] public string BackgroundImagePath;
    }
}