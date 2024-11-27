
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public class PlayerPointsState : SharedStateComponent {
        public ReviewModule ReviewModule;
        public Timer ReviewTimer;

        public int ActivePips;
        [HideInInspector] public bool SubmittedPuzzle = false;
        [HideInInspector] public bool SubmittedObject = false;
        [HideInInspector] public int SciencePoints = 0;
    }



    public static partial class PointsUtility {
        public static int GetPoints() {
            return Find.State<PlayerPointsState>().SciencePoints;
        }

        public static void SetPoints(int newPoints) {
            PlayerPointsState state = Find.State<PlayerPointsState>();
            state.SciencePoints = newPoints;
            UpdatePointDisplay(state);
        }
    }
}