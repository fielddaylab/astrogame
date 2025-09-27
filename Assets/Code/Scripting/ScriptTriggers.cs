using Astro.Audio;
using Astro.Save;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.HID;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.UI;
using Leaf.Runtime;
using System.Collections;
using UnityEngine;

namespace Astro {
    public static class ScriptTriggers {
        [InvokeOnBoot]
        static public void Init() {
            ReviewUtility.OnPointsUpdated.Register(OnScore);
            ReviewUtility.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);

            Game.Events.Register(GameEvents.NeutrinoNavigationComplete, OnNeutrinoNavComplete);

            Game.Events.Register(GameEvents.ValidOpenIdSubmission, OnValidOpenIdSubmission);
            Game.Events.Register(GameEvents.ValidKnowledgeSubmission, OnValidKnowledgeSubmission);
            Game.Events.Register(GameEvents.InvalidOpenIdSubmission, OnInvalidOpenIdSubmission);
            Game.Events.Register(GameEvents.IncorrectOpenIdSubmission, OnIncorrectOpenIdSubmission);
            Game.Events.Register(GameEvents.DuplicateOpenIdSubmission, OnDuplicateOpenIdSubmission);
            Game.Events.Register(GameEvents.UnacceptedOpenIdSubmission, OnUnacceptedOpenIdSubmission);
            Game.Events.Register(GameEvents.ClassificationClicked, OnClassificationClicked);
        }

        static private void OnScore() {
            using (var table = TempVarTable.Alloc()) {
                table.Set("sciencePoints", ReviewUtility.GetPoints());
                table.Set("openIDThresholdHit", ReviewUtility.GetPoints() >= DayConfigUtil.GetConfigForState().NumNeutrinoPoints);
                ScriptUtility.Trigger(ScriptEvents.PointsUpdated, table);
            }
        }

        static private void OnCorrectPuzzleSubmit() {
            using (var table = TempVarTable.Alloc()) {
                table.Set("puzzleName", Find.State<PuzzleState>().ActivePuzzle.DisplayName);
                ScriptUtility.Trigger(ScriptEvents.CorrectPuzzleSubmission, table);
            }
        }

        static private void OnNeutrinoNavComplete() {
            ScriptUtility.Trigger(ScriptEvents.NeutrinoNavigationComplete);
        }

        static private void OnValidOpenIdSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnValidOpenIdSubmission);
        }

        static private void OnValidKnowledgeSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnValidKnowledgeSubmission);
        }

        static private void OnIncorrectOpenIdSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnIncorrectOpenIdSubmission);
        }

        static private void OnInvalidOpenIdSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnInvalidOpenIdSubmission);
        }

        static private void OnDuplicateOpenIdSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnDuplicateOpenIdSubmission);
        }

        static private void OnUnacceptedOpenIdSubmission() {
            ScriptUtility.Trigger(ScriptEvents.OnUnacceptedOpenIdSubmission);
        }

        static private void OnClassificationClicked() {
            ScriptUtility.Trigger(ScriptEvents.OnClassificationClicked);
        }

        // TODO make this actually process more than one day
        [LeafMember("LoadNextDay")]
        static public void LoadNextDay(StringHash32 transitionType = default) {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            Game.Events.Dispatch(GameEvents.BeforeNextDayLoad);

            state.DayIndex += 1;
            DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[state.DayIndex]);
#if DEVELOPMENT
            Log.Msg("[ScriptTriggers > LoadNextDay] Loading day '{0}'", day.name);
#endif // DEVELOPMENT

            MusicUtility.StopMusic(1);

            SaveUtility.Save(SaveSlot.Main);

            Game.Scenes.LoadMainScene(day.Scene, true, new MainSceneTransitionArgs() {
                TransitionType = transitionType
            });
        }

        [LeafMember("LoadDay")]
        static public void LoadDay(StringHash32 dayId, StringHash32 transitionType = default, bool ignoreSave = false) {
            PlayerProgressState state = Find.State<PlayerProgressState>();
            StoryAsset story = Find.GlobalAsset<StoryAsset>();
            Game.Events.Dispatch(GameEvents.BeforeNextDayLoad);

            for (int i = 0; i < story.Days.Length; i++) {
                if (story.Days[i] == dayId) {
                    state.DayIndex = i;
                    break;
                }
            }

            var day = Find.NamedAsset<DayConfigAsset>(dayId);
            Log.Msg("[ScriptTriggers > LoadDay] Loading day '{0}'", day.name);

            MusicUtility.StopMusic(1);

            state.CompletedPrelude = true;
            if (!ignoreSave) {
                SaveUtility.Save(SaveSlot.Main);
            }

            Game.Scenes.LoadMainScene(day.Scene, true, new MainSceneTransitionArgs() {
                TransitionType = transitionType
            });
        }

        [LeafMember("SetInputState")]
        static private void LeafSetInputState(bool enabled) {
            var state = Find.State<InputState>();
            InputUtility.SetInputEnabled(state, enabled);

            if (enabled) {
                Routine.Start(CameraRigUtility.SetLetterboxEnabled(false));
                GameLoop.ResumeUpdates(AstroGame.InteractUpdateMask);
            } else {
                GameLoop.SuspendUpdates(AstroGame.InteractUpdateMask);
                CursorHint.Unlock(CursorHint.Current);
                Routine.Start(CameraRigUtility.SetLetterboxEnabled());
            }
        }

        [LeafMember("StartPuzzleMode")]
        static private void LeafStartPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StartPuzzleMode);
            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.PuzzleSubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.AnySubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask);
            SlotHighlightUtility.SetInstrumentButtonsDimmed(Find.State<InstrumentInventoryState>(), false);
        }

        [LeafMember("StartFinalPuzzleMode")]
        static private void LeafStartFinalPuzzle() {
            Game.Events.Dispatch(GameEvents.StartFinalPuzzle);
            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.PuzzleSubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.AnySubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask); 
        }

        [LeafMember("StopPuzzleMode")]
        static private void LeafStopPuzzleMode() {
            Game.Events.Dispatch(GameEvents.StopPuzzleMode);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.PuzzleSubmissionUpdateMask);
            if (GameLoop.IsSuspended(AstroGame.OpenSubmissionUpdateMask)) {
                GameLoop.SuspendUpdates(AstroGame.AnySubmissionUpdateMask);
            }
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
        static private void LeafStartOpenMode() {
            Game.Events.Dispatch(GameEvents.StartOpenMode);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.OpenSubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.AnySubmissionUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask);
            SlotHighlightUtility.SetInstrumentButtonsDimmed(Find.State<InstrumentInventoryState>(), true);
        }

        [LeafMember("UpdateOpenIdSubmissions")]
        static private void LeafUpdateOpenIdSubmissions() {
            Game.Events.Dispatch(GameEvents.UpdateOpenIdSubmission);
        }

        [LeafMember("StopOpenMode")]
        static private void LeafStopOpenMode() {
            Game.Events.Dispatch(GameEvents.StopOpenMode);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.OpenSubmissionUpdateMask);
            if (GameLoop.IsSuspended(AstroGame.PuzzleSubmissionUpdateMask)) {
                GameLoop.SuspendUpdates(AstroGame.AnySubmissionUpdateMask);
            }
            GameLoop.SuspendUpdates(AstroGame.InstrumentUpdateMask);

            ViewState viewState = Find.State<ViewState>();
            var targetNode = ViewNavUtility.GetNodeById("Right");

            ViewNavUtility.MoveToNode(viewState, targetNode);
        }

        [LeafMember("StartNeutrinoNavigation")]
        static private void LeafStartNeutrinoNavigation() {
            // whenever we start neutrino nav we should insure there is nothing highlighted and no data displayed
            LeafClearMonitorSelection();

            Game.Events.Dispatch(GameEvents.StartNeutrinoNavigation);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("StopNeutrinoNavigation")]
        static private void LeafStopNeutrinoNavigation() {
            Game.Events.Dispatch(GameEvents.StopNeutrinoNavigation);

            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("AlignCamToNeutrino")]
        static private IEnumerator LeafAlignCamToNeutrino() {
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            if (!config) yield break;
            EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;

            var navState = Find.State<NavigationState>();
            yield return navState.ConstellationSnapRoutine.Replace(NavigationUtility.SnapAlignment(target));
        }

        [LeafMember("StartPuzzleNavigation")]
        static private void LeafStartPuzzleNavigation() {
            Game.Events.Dispatch(GameEvents.StartPuzzleNavigation);

            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        // [LeafMember("StopPuzzleNavigation")]
        // static private void LeafStopPuzzleNavigation() {
        //     Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
        // }

        [LeafMember("ClearMonitorSelection")]
        static private void LeafClearMonitorSelection() {
            Game.Events.Dispatch(GameEvents.MonitorEmptySpaceClicked);
            GameLoop.ResumeUpdates(AstroGame.InteractUpdateMask);
            GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("InitUpdateMasks")]
        static private void LeafInitUpdateMasks() {
            GameLoop.SuspendUpdates(AstroGame.InteractUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.OpenSubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.PuzzleSubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.AnySubmissionUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.DocumentUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.InstrumentUpdateMask);
            GameLoop.SuspendUpdates(AstroGame.MonitorControlsUpdateMask);
        }

        [LeafMember("ToggleDeskPicture")]
        static private void LeafToggleDeskPicture() {
            BackgroundState bgState = Find.State<BackgroundState>();
            bgState.DeskPicture.SetActive(!bgState.DeskPicture.activeSelf)
            ;
        }

        [LeafMember("EnableDocumentClose")]
        static private void LeafEnableDocumentClose(StringHash32 assetId, bool value, bool updateNow = false) {
            DocumentBoardState boardState = Find.State<DocumentBoardState>();

            boardState.DocumentCloseEnabledState[assetId] = value;

            if (!updateNow) return;

            DocumentAsset docAsset = Find.NamedAsset<DocumentAsset>(assetId);
            if (docAsset == null) {
                Debug.LogWarning("[LeafEnableDocumentClose] Failed to find document" + assetId);
                return;
            }

            if (boardState.DocZoomed) {
                DocumentUtility.UpdateEnabledDocParts(boardState.DocZoomed, DocumentBoardState.ZoomActiveFunctions);
            } else {
                DocumentUtility.UpdateEnabledDocParts(docAsset.Interactable, DocumentBoardState.BoardActiveFunctions); 
            }
        }
    }
}