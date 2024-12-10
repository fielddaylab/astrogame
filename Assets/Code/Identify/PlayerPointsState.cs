
using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class PlayerPointsState : SharedStateComponent {
        public ReviewModule ReviewModule;
        public Timer ReviewTimer;
        public Timer ReviewCooldown;

        public Sprite PipCorrect;
        public Sprite PipIncorrect;

        [HideInInspector] public int ActivePips;
        [HideInInspector] public bool SubmittedPuzzle = false;
        [HideInInspector] public bool SubmittedObject = false;
        [HideInInspector] public int SciencePoints = 0;
    }



    public static partial class PointsUtility {

        public static bool ReviewInProgress(PlayerPointsState state = null) {
            if (state == null) {
                state = Find.State<PlayerPointsState>();
            }
            return state.ReviewTimer.GetProgress() > 0 || state.ReviewCooldown.GetProgress() > 0;
        }
        public static int GetPoints(PlayerPointsState state = null) {
            if (state == null) {
                state = Find.State<PlayerPointsState>();
            }
            return state.SciencePoints;
        }

        public static void SetPoints(int newPoints, PlayerPointsState state = null) {
            if (state == null) {
                state = Find.State<PlayerPointsState>();
            }
            state.SciencePoints = newPoints;
            UpdatePointDisplay(state);
        }

        public static void AddPoints(int delta, PlayerPointsState state = null) {
            if (state == null) {
                state = Find.State<PlayerPointsState>();
            }
            int newTotal = state.SciencePoints + delta;
            SetPoints(Math.Max(0, newTotal), state);
        }
    }
}