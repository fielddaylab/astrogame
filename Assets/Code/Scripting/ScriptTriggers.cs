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
            ReviewUtility.OnPointsUpdated.Register(OnScore);
            ReviewUtility.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);

            Game.Events.Register(GameEvents.PuzzleNavigationComplete, OnPuzzleNavComplete);
            Game.Events.Register(GameEvents.NeutrinoNavigationComplete, OnNeutrinoNavComplete);
        }

        static private void OnScore() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("sciencePoints", ReviewUtility.GetPoints());
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

        static public void LoadDay(StringHash32 dayId) {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            for (int i = 0; i < story.Days.Length; i++) {
                if (story.Days[i] == dayId){
                    state.DayIndex = i;
                    break;
                }
            }

            var day = Find.NamedAsset<DayConfigAsset>(dayId);

            Game.Events.Dispatch(GameEvents.BeforeNextDayLoad);

            Log.Msg("[ScriptTriggers] Loading day '{0}'", day.name);

            Game.Scenes.LoadMainScene(day.Scene, true);
        }

        [LeafMember("SetInputState")]
        static private void LeafSetInputState(bool enabled) {
            var state = Find.State<InputState>();
            InputUtility.SetInputEnabled(state, enabled);

            if (enabled) {
                GameLoop.ResumeUpdates(AstroGame.InteractUpdateMask);
            }
            else {
                GameLoop.SuspendUpdates(AstroGame.InteractUpdateMask);
            }
        }

        [LeafMember("StartPuzzleMode")]
        static private void LeafStartPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StartPuzzleMode);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.SubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask);
        }

        [LeafMember("StopPuzzleMode")]
        static private void LeafStopPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StopPuzzleMode);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.SubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.InstrumentUpdateMask);
        }

        
        [LeafMember("StartMonitorControls")]
        static private void LeafStartMonitorControls() {
            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("StopMonitorControls")]
        static private void LeafStopMonitorControls()
        {
            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("LockMonitorFocus")]
        static private void LeafLockMonitorFocus()
        {
            Game.Events.Dispatch(GameEvents.LockMonitorFocus);
        }

        [LeafMember("UnlockMonitorFocus")]
        static private void LeafUnlockMonitorFocus()
        {
            Game.Events.Dispatch(GameEvents.UnlockMonitorFocus);
        }

        [LeafMember("StartOpenMode")]
        static private void LeafStartOpenMode(){
            Game.Events.Dispatch(GameEvents.StartOpenMode);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.SubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask);
            SlotHighlightUtility.SetInstrumentButtonsDimmed(Find.State<InstrumentInventoryState>(), true);
        }

        [LeafMember("StopOpenMode")]
        static private void LeafStopOpenMode(){
            Game.Events.Dispatch(GameEvents.StopOpenMode);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.SubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.InstrumentUpdateMask);
            SlotHighlightUtility.SetInstrumentButtonsDimmed(Find.State<InstrumentInventoryState>(), false);
        }

        [LeafMember("StartNeutrinoNavigation")]
        static private void LeafStartNeutrinoNavigation() {
            Game.Events.Dispatch(GameEvents.StartNeutrinoNavigation);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("StopNeutrinoNavigation")]
        static private void LeafStopNeutrinoNavigation() {
            Game.Events.Dispatch(GameEvents.StopNeutrinoNavigation);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("AlignCamToNeutrino")]
        static private void LeafAlignCamToNeutrino()
        {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            if (!config) return;
            EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;

            var navState = Find.State<PuzzleNavigationState>();
            navState.ConstellationSnapRoutine.Replace(PuzzleNavigationUtility.SnapConstellationAlignment(target));
        }

        [LeafMember("StartPuzzleNavigation")]
        static private void LeafStartPuzzleNavigation() {
            Game.Events.Dispatch(GameEvents.StartPuzzleNavigation);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("StopPuzzleNavigation")]
        static private void LeafStopPuzzleNavigation() {
            Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("ClearMonitorSelection")]
        static private void LeafClearMonitorSelection() {
            Game.Events.Dispatch(GameEvents.MonitorEmptySpaceClicked);
        }

        [LeafMember("InitUpdateMasks")]
        static private void LeafInitUpdateMasks()
        {
            GameLoop.SuspendUpdates(AstroGame.InteractUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.SubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.DocumentUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.InstrumentUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }
    }
}