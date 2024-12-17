
using FieldDay.Assets;
using UnityEngine;

namespace Astro {

    [CreateAssetMenu(menuName = "AstroGame/Reference/Page")]
    public class ReferencePageAsset : NamedAsset {
        public string TitleLeft;
        public ReferenceEntry[] EntriesLeft;
        public string TitleRight;
        public ReferenceEntry[] EntriesRight;
        public Sprite BackgroundSprite;
    }
}