using BeauUtil;
using FieldDay.Assets;
using Leaf;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Day Config")] 
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;

        public PuzzleAsset Puzzle;
    }
}