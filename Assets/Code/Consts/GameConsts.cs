

using Astro;
using BeauUtil;

public static class GameEvents {
    public static readonly StringHash32 DocumentSelected = "input:document-selected";
    public static readonly StringHash32 MonitorEmptySpaceClicked = "input:monitor-empty-space-clicked";
    public static readonly StringHash32 StartOpenMode = "start-open-mode";
    public static readonly StringHash32 ValidOpenIdSubmission = "valid-open-id-submission";
    public static readonly StringHash32 ValidKnowledgeSubmission = "valid-knowledge-submission";
    public static readonly StringHash32 UpdateOpenIdSubmission = "update-open-id-submission";
    public static readonly StringHash32 InvalidOpenIdSubmission = "invalid-open-id-submission";
    public static readonly StringHash32 DuplicateOpenIdSubmission = "duplicate-open-id-submission";
    public static readonly StringHash32 IncorrectOpenIdSubmission = "incorrect-open-id-submission";
    public static readonly StringHash32 UnacceptedOpenIdSubmission = "unaccepted-open-id-submission";
    public static readonly StringHash32 ClassificationClicked = "classification-clicked";
    public static readonly StringHash32 StopOpenMode = "stop-open-mode";
    public static readonly StringHash32 StartPuzzleMode = "start-puzzle-mode";
    public static readonly StringHash32 AfterPuzzleModeStart = "after-puzzle-mode-start";
    public static readonly StringHash32 StopPuzzleMode = "stop-puzzle-mode";
    public static readonly StringHash32 StartNeutrinoNavigation = "start-navigation-mode";
    public static readonly StringHash32 NeutrinoNavigationComplete = "neutrino-navigation-complete";
    public static readonly StringHash32 StopNeutrinoNavigation = "stop-navigation-mode";
    public static readonly StringHash32 StartPuzzleNavigation = "start-constellation-mode";
    public static readonly StringHash32 PuzzleNavigationComplete = "puzzle-navigation-complete";
    public static readonly StringHash32 StopPuzzleNavigation = "stop-constellation-mode";
    public static readonly StringHash32 LockMonitorFocus = "lock-monitor-focus";
    public static readonly StringHash32 UnlockMonitorFocus = "unlock-monitor-focus";
    public static readonly StringHash32 BeforeNextDayLoad = "before-next-day-load"; 
    public static readonly StringHash32 StartFinalPuzzle = "start-final-puzzle";

    public static readonly StringHash32 GamePaused = "game-paused";
    public static readonly StringHash32 GameResumed = "game-resumed";

    public static readonly StringHash32 MonitorSwitchedFilter = "monitor-switched-filter";
    public static readonly StringHash32 OnStarSelected = "monitor-star-selected";
    public static readonly StringHash32 InstrumentUnlocked = "instrument-unlocked";

    public static readonly StringHash32 TitleGameStarting = "title-game-starting";

    public static readonly StringHash32 TryCreateNewName = "try-create-new-name";
    public static readonly StringHash32 TitleErrorReceived = "title-error-received";

    public static readonly StringHash32 ProfileSaveBegin = "profile-save-begin";
    public static readonly StringHash32 ProfileSaveError = "profile-save-error";
    public static readonly StringHash32 ProfileSaveSuccess = "profile-save-success";
    public static readonly StringHash32 ProfileSaveAttemptCompleted = "profile-save-attempt-completed";

    // Additional Analytics
    public static readonly StringHash32 TitleNewGameClicked = "title-new-game-clicked";
    public static readonly StringHash32 TitleContinueGameClicked = "title-continue-game-clicked";
    public static readonly StringHash32 TitleOptionsClicked = "title-options-game-clicked";
    public static readonly StringHash32 GameStart = "game-start"; // bool fromResume
    public static readonly StringHash32 LevelStart = "level-start"; // int levelNum
    public static readonly StringHash32 LevelEnd = "level-end"; // int levelNum
    public static readonly StringHash32 ClickPauseGame = "click-pause-game";
    public static readonly StringHash32 ClickResumeGame = "click-resume-game";
    public static readonly StringHash32 CutsceneStart = "cutscene-start"; // string cutsceneId
    public static readonly StringHash32 CutsceneEnd = "cutscene-end"; // string custceneId
    public static readonly StringHash32 SubtitleDataChanged = "subtitle-data-changed"; // SubtitleLogData data
    public static readonly StringHash32 DialogueAudioStart = "dialogue-audio-start";
    public static readonly StringHash32 DialogueAudioEnd = "dialogue-audio-end";
    public static readonly StringHash32 DialogueTextDisplayed = "dialogue-text-displayed";
    public static readonly StringHash32 ClickSkipDialogueLine = "click-skip-dialogue-line";
    public static readonly StringHash32 HintChanged = "hint-changed"; 
    public static readonly StringHash32 HintDisplayed = "hint-displayed";
    public static readonly StringHash32 HintHidden = "hint-hidden"; 
    public static readonly StringHash32 MeteorAreaAssigned = "meteor-area-assigned";
    public static readonly StringHash32 MeteorAreaHighlighted = "meteor-area-highlighted";
    public static readonly StringHash32 MeteorAreaUnhighlighted = "meteor-area-unhighlighted";
    public static readonly StringHash32 StarHighlighted = "star-highlighted";
    public static readonly StringHash32 StarUnhighlighted = "star-unhighlighted";
    public static readonly StringHash32 HoverStar = "hover-star";
    public static readonly StringHash32 StarClicked = "star-clicked";
    public static readonly StringHash32 TelescopeTurned = "telescope-turned";
    public static readonly StringHash32 TelescopeViewAssigned = "telescope-view-assigned";
    public static readonly StringHash32 LocatorCloser = "locator-closer";
    public static readonly StringHash32 LocatorFurther = "locator-further";
    public static readonly StringHash32 FoundTelescopeView = "found-telescope-view";
    public static readonly StringHash32 ConstellationIdAssigned = "constellation-id_assigned";
    public static readonly StringHash32 RefGuideControlPageChanged = "ref-guide-control-page-changed";
    public static readonly StringHash32 ClickRefGuideOpened = "click-ref-guide-opened";
    public static readonly StringHash32 ClickRefGuideClosed = "click-ref-guide-closed";
    public static readonly StringHash32 RefGuideZoomed = "ref-guide-zoomed";
    public static readonly StringHash32 RefGuideUnzoomed = "ref-guide-unzoomed";
    public static readonly StringHash32 SelectRefGuideTab = "select-ref-guide-tab"; // string LabelText
    public static readonly StringHash32 TurnRefGuidePage = "turn-ref-guide-page"; // bool isLeft
    public static readonly StringHash32 SelectClassification = "select-classification"; // ClassificationLogData
    public static readonly StringHash32 ToggleSpectralElement = "toggle-spectral-element"; // bool toggledOn
    public static readonly StringHash32 ClickSubmitStarId = "click-submit-star-id";
    public static readonly StringHash32 SubmittedStarChanged = "submitted-star-changed";
    public static readonly StringHash32 PointsUpdated = "points-updated"; // int pointsEarned
    public static readonly StringHash32 StartAdjustRadio = "start-adjust-radio"; // int start frequency
    public static readonly StringHash32 EndAdjustRadio = "end-adjust-radio"; // int end frequency
    public static readonly StringHash32 RadioSecretFound = "radio-secret-found"; // msgId
    public static readonly StringHash32 TelescopeStencilDisplayed = "telescope-stencil-displayed";
    public static readonly StringHash32 ClickToolLoad = "click-tool-load"; // PacketTransferData
    public static readonly StringHash32 SelectPuzzleCell = "select-puzzle-cell"; // PacketTransferData
    public static readonly StringHash32 TransferValueToCell = "transfer-value-to-cell"; // PacketTransferData
}

public static class ScriptEvents {
    public static readonly StringHash32 OpenModeStart = "OpenModeStart";
    public static readonly StringHash32 PuzzleModeStart = "PuzzleModeStart";
    public static readonly StringHash32 PointsUpdated = "PointsUpdated";
    public static readonly StringHash32 NeutrinoNavigationComplete = "NeutrinoNavigationComplete";
    public static readonly StringHash32 PuzzleNavigationComplete = "PuzzleNavigationComplete";
    public static readonly StringHash32 OnPuzzleGridFullyPopulated = "OnPuzzleGridFullyPopulated";
    public static readonly StringHash32 OnPuzzleCellSelected = "OnPuzzleCellSelected";
    public static readonly StringHash32 OnPuzzleCellFilled = "OnPuzzleCellFilled";
    public static readonly StringHash32 OnPuzzleRowFilled = "OnPuzzleRowFilled";
    public static readonly StringHash32 IncorrectPuzzleSubmission = "IncorrectPuzzleSubmission";
    public static readonly StringHash32 CorrectPuzzleSubmission = "CorrectPuzzleSubmission";
    public static readonly StringHash32 DocumentInspectStart = "DocumentInspectStart";
    public static readonly StringHash32 DocumentInspectFlip = "DocumentInspectFlip";
    public static readonly StringHash32 DocumentInspectEnd = "DocumentInspectEnd";
    public static readonly StringHash32 DocumentPuzzlePromptStart = "DocumentPuzzlePromptStart";
    public static readonly StringHash32 DocumentPuzzleSolved = "DocumentPuzzleSolved";
    public static readonly StringHash32 CutsceneBegin = "CutsceneBegin";
    public static readonly StringHash32 CutsceneEnd = "CutsceneEnd";
    public static readonly StringHash32 OnTelescopeMoved = "OnTelescopeMoved";
    public static readonly StringHash32 OnLabInteraction = "OnLabInteraction";
    public static readonly StringHash32 OnStarSelected = "OnStarSelected";
    public static readonly StringHash32 OnNeutrinoStarSelected = "OnNeutrinoStarSelected";
    public static readonly StringHash32 OnRefGuideOpened = "OnRefGuideOpened";
    public static readonly StringHash32 OnRefGuideClosed = "OnRefGuideClosed";
    public static readonly StringHash32 OnValidOpenIdSubmission = "OnValidOpenIdSubmission";
    public static readonly StringHash32 OnValidKnowledgeSubmission = "OnValidKnowledgeSubmission";
    public static readonly StringHash32 OnInvalidOpenIdSubmission = "OnInvalidOpenIdSubmission";
    public static readonly StringHash32 OnDuplicateOpenIdSubmission = "OnDuplicateOpenIdSubmission";
    public static readonly StringHash32 OnIncorrectOpenIdSubmission = "OnIncorrectOpenIdSubmission";
    public static readonly StringHash32 OnUnacceptedOpenIdSubmission = "OnUnacceptedOpenIdSubmission";
    public static readonly StringHash32 OnClassificationClicked = "OnClassificationClicked";
    public static readonly StringHash32 OnNeutrinoNavWarmer = "OnNeutrinoNavWarmer";
    public static readonly StringHash32 OnNeutrinoNavColder = "OnNeutrinoNavColder";
    public static readonly StringHash32 OnLeaveNeutrinoRegion = "OnLeaveNeutrinoRegion";
    public static readonly StringHash32 OnConstellationNavWarmer = "OnConstellationNavWarmer";
    public static readonly StringHash32 OnConstellationNavColder = "OnConstellationNavColder";
    public static readonly StringHash32 BeginDecoderSequence = "BeginDecoderSequence";
    public static readonly StringHash32 OnDecoderSuccess = "OnDecoderSuccess";
    public static readonly StringHash32 OnDecoderInteract = "OnDecoderInteract";

    public static readonly StringHash32 RadioChannelListenStart = "RadioChannelListenStart"; // player started listening
    public static readonly StringHash32 RadioChannelListenEnd = "RadioChannelListenEnd"; // player stopped listening
    public static readonly StringHash32 RadioChannelFinished = "RadioChannelFinished"; // reached end of radio channel
}

public static class DataTypeLabels
{
    public static readonly string Name = "Name";
    public static readonly string Coordinates = "Coords";
    public static readonly string Color = "Color";
    public static readonly string ApparentMagnitude = "App. Magnitude";
    public static readonly string AbsoluteMagnitude = "Abs. Magnitude";
    public static readonly string MaterialSpectrum = "Material Spectrum";
    public static readonly string Temperature = "Temperature";
    public static readonly string Distance = "Distance";
    public static readonly string HistoricalCoordinates = "H. Coords";
    public static readonly string HistoricalApparentMagnitude = "H. App. Magnitude";
    public static readonly string BlueMagnitude = "Blue Magnitude";
    public static readonly string InfraredMagnitude = "IR Magnitude";
    public static readonly string HistoricalColor = "H. Color";
}