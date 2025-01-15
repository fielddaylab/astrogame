
using FieldDay.Assets;
using UnityEngine;

namespace Astro {

    [CreateAssetMenu(menuName = "AstroGame/Reference/Page")]
    public class ReferencePageAsset : NamedAsset {
        public string TitleLeft;
        public ReferenceClassification[] EntriesLeft;
        public string TitleRight;
        public ReferenceClassification[] EntriesRight;
        public Sprite BackgroundSprite;
    }
}