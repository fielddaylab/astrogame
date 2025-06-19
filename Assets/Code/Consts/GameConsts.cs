

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
    public static readonly StringHash32 StopOpenMode = "stop-open-mode";
    public static readonly StringHash32 StartPuzzleMode = "start-puzzle-mode";
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

    public static readonly StringHash32 MonitorSwitchedFilter = "monitor-switched-filter";
    public static readonly StringHash32 OnStarSelected = "monitor-star-selected";
    public static readonly StringHash32 InstrumentUnlocked = "instrument-unlocked";
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
    public static readonly StringHash32 OnNeutrinoNavWarmer = "OnNeutrinoNavWarmer";
    public static readonly StringHash32 OnNeutrinoNavColder = "OnNeutrinoNavColder";
    public static readonly StringHash32 OnLeaveNeutrinoRegion = "OnLeaveNeutrinoRegion";
    public static readonly StringHash32 OnConstellationNavWarmer = "OnConstellationNavWarmer";
    public static readonly StringHash32 OnConstellationNavColder = "OnConstellationNavColder";
    public static readonly StringHash32 BeginDecoderSequence = "BeginDecoderSequence";
    public static readonly StringHash32 OnDecoderSuccess = "OnDecoderSuccess";

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