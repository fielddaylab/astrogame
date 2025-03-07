using UnityEngine;
using FieldDay.Assets;
using BeauUtil;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/StoryAsset")]
    public sealed class StoryAsset : GlobalAsset {
        [AssetName(typeof(DayConfigAsset))] public StringHash32[] Days;
    }
}