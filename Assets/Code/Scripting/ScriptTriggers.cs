using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using Leaf.Runtime;

namespace Astro {
    public static class ScriptTriggers {
        [InvokeOnBoot]
        static public void Init() {
            PointsUtility.OnPointsUpdated.Register(OnScore);
            PointsReviewSystem.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);

            Game.Events.Register(GameEvents.PuzzleNavigationComplete, OnPuzzleNavComplete);
        }

        static private void OnScore() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("sciencePoints", PointsUtility.GetPoints());
                ScriptUtility.Trigger(ScriptEvents.PointsUpdated, table);
            }
        }

        static private void OnCorrectPuzzleSubmit() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("puzzleName", Find.State<PuzzleState>().ActivePuzzle.DisplayName);
                ScriptUtility.Trigger(ScriptEvents.CorrectPuzzleSubmission, table);
            }
        }

        static private void OnPuzzleNavComplete() {
            ScriptUtility.Trigger(ScriptEvents.PuzzleNavigationComplete);
        }

        [LeafMember("SetInputState")]
        static private void LeafSetInputState(bool enabled) {
            var state = Find.State<InputState>();
            InputUtility.SetInputEnabled(state, enabled);
        }

        [LeafMember("StartPuzzleMode")]
        static private void LeafStartPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StartPuzzleMode);
        }

        [LeafMember("StopPuzzleMode")]
        static private void LeafStopPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StopPuzzleMode);
        }

        [LeafMember("StartOpenMode")]
        static private void LeafStartOpenMode(){
            Game.Events.Dispatch(GameEvents.StartOpenMode);
        }

        [LeafMember("StopOpenMode")]
        static private void LeafStopOpenMode(){
            Game.Events.Dispatch(GameEvents.StopOpenMode);
        }

        [LeafMember("StartPuzzleNavigation")]
        static private void LeafStartPuzzleNavigation() {
            Game.Events.Dispatch(GameEvents.StartPuzzleNavigation);
        }

        [LeafMember("StopPuzzleNavigation")]
        static private void LeafStopPuzzleNavigation() {
            Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
        }
        
        [LeafMember("ClearMonitorSelection")]
        static private void LeafClearMonitorSelection() {
            Game.Events.Dispatch(GameEvents.MonitorEmptySpaceClicked);
        }
    }
}