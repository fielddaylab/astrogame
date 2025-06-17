using Leaf;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using Astro.Radio;
using UnityEngine;
using System.Collections.Generic;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Day Config")] 
    public sealed class DayConfigAsset : NamedAsset {
        public SceneReference Scene;
        public LeafAsset[] Scripts;

        [Header("Sky")]
        public HmsCoords SkyRotationOffset;

        [Header("Puzzle")]
        public PuzzleAsset DayPuzzle;

        [Header("Console")]
        public List<ConsoleTextAsset> ConsoleTextAssets;

        [Header("Open Identification")]
        [Tooltip("Controls how many points are displayed on the clearance modal")][Range(1,7)]
        public int NumNeutrinoPoints;
        public NeutrinoConfigAsset NeutrinoEvent;
        public ClassificationTypeMask AcceptedIDSubmissions = ClassificationTypeMask.ALL;

        [Header("Misc")]
        public RadioChannelSet RadioChannels;
    }

    static public class DayConfigUtil {
        static public DayConfigAsset GetConfigForState() {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();

#if DEVELOPMENT
            if (state.LoadDebugScene && story.DEBUG_SandboxDay) {
                return story.DEBUG_SandboxDay;
            }
#endif // DEVELOPMENT

            StringHash32 configId = story.Days[state.DayIndex];
            return Find.NamedAsset<DayConfigAsset>(configId);
        }

    }
}