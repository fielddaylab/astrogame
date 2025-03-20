using System.Collections;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Scripting;
using Leaf.Runtime;

namespace Astro {
    public static class ScriptTriggers {
        [InvokeOnBoot]
        static public void Init() {
            PointsUtility.OnPointsUpdated.Register(OnScore);
            PointsReviewSystem.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);

            Game.Events.Register(GameEvents.PuzzleNavigationComplete, OnPuzzleNavComplete);
            Game.Events.Register(GameEvents.NeutrinoNavigationComplete, OnNeutrinoNavComplete);
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

        static private void OnNeutrinoNavComplete() {
            ScriptUtility.Trigger(ScriptEvents.NeutrinoNavigationComplete);
        }

        // TODO make this actually process more than one day
        [LeafMember("LoadNextDay")]
        static public void LoadNextDay() {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();

            Game.Events.Dispatch(GameEvents.BeforeNextDayLoad);

            state.DayIndex += 1;
            DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[state.DayIndex]);
            Log.Msg("[ScriptTriggers] Loading day '{0}'", day.name);

            Game.Scenes.LoadMainScene(day.Scene, true);
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

        [LeafMember("StartNeutrinoNavigation")]
        static private void LeafStartNeutrinoNavigation() {
            Game.Events.Dispatch(GameEvents.StartNeutrinoNavigation);
        }
        
        [LeafMember("StopNeutrinoNavigation")]
        static private void LeafStopNeutrinoNavigation() {
            Game.Events.Dispatch(GameEvents.StopNeutrinoNavigation);
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