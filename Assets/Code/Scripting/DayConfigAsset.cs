using Astro.Radio;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Leaf;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Day Config")] 
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;
        public HmsCoords SkyRotationOffset;

        public PuzzleAsset DayPuzzle;
        public NeutrinoConfigAsset NeutrinoEvent;
        public ClassificationTypeMask AcceptedIDSubmissions = ClassificationTypeMask.ALL;
        public RadioChannelSet RadioChannels;
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