
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {

    [CreateAssetMenu(menuName = "AstroGame/Reference/Page")]
    public class ReferencePageAsset : NamedAsset {
        public ReferenceEntry[] EntriesLeft;
        public ReferenceEntry[] EntriesRight;
        public Sprite BackgroundSprite;
    }
}