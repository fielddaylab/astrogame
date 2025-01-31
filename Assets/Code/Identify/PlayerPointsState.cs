
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class PlayerPointsState : SharedStateComponent {
        public ReviewModule ReviewModule;
        public Timer ReviewTimer;
        public Timer ReviewCooldown;

        [NonSerialized] public int ActivePips;
        [NonSerialized] public bool SubmittedPuzzle = false;
        [NonSerialized] public bool SubmittedObject = false;
        [NonSerialized] public int SciencePoints = 0;
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
            OnPointsUpdated.Invoke(newPoints);
            UpdatePointDisplay(state);
        }

        public static void AddPoints(int delta, PlayerPointsState state = null) {
            if (state == null) {
                state = Find.State<PlayerPointsState>();
            }
            int newTotal = state.SciencePoints + delta;
            SetPoints(Math.Max(0, newTotal), state);
        }

        [DebugMenuFactory]
        private static DMInfo PointsMenu() {
            DMInfo info = new DMInfo("Points");
            info.AddButton("Add Point", () => {
                AddPoints(1);
            });
            return info;
        }

        static public readonly CastableEvent<int> OnPointsUpdated = new CastableEvent<int>();
    }
}