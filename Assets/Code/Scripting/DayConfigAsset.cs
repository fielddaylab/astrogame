using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Leaf;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Day Config")] 
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;

        public PuzzleAsset DayPuzzle;
        public NeutrinoConfigAsset NeutrinoEvent;
    }

    static public class DayConfigUtil {
        static public DayConfigAsset GetConfigForState() {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();

            StringHash32 configId = story.Days[state.DayIndex];
            return Find.NamedAsset<DayConfigAsset>(configId);
        }

    }
}