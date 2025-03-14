

using Astro;
using BeauUtil;

public static class GameEvents {
    public static readonly StringHash32 DocumentSelected = "input:document-selected";
    public static readonly StringHash32 MonitorEmptySpaceClicked = "input:monitor-empty-space-clicked";
    public static readonly StringHash32 StartOpenMode = "start-open-mode";
    public static readonly StringHash32 StopOpenMode = "stop-open-mode";
    public static readonly StringHash32 StartPuzzleMode = "start-puzzle-mode";
    public static readonly StringHash32 StopPuzzleMode = "stop-puzzle-mode";
    public static readonly StringHash32 StartNeutrinoNavigation = "start-navigation-mode";
    public static readonly StringHash32 StopNeutrinoNavigation = "stop-navigation-mode";
    public static readonly StringHash32 StartPuzzleNavigation = "start-constellation-mode";
    public static readonly StringHash32 PuzzleNavigationComplete = "puzzle-navigation-complete";
    public static readonly StringHash32 StopPuzzleNavigation = "stop-constellation-mode";
}

public static class ScriptEvents {
    public static readonly StringHash32 OpenModeStart = "OpenModeStart";
    public static readonly StringHash32 PuzzleModeStart = "PuzzleModeStart";
    public static readonly StringHash32 PointsUpdated = "PointsUpdated";
    public static readonly StringHash32 PuzzleNavigationComplete = "PuzzleNavigationComplete";
    public static readonly StringHash32 CorrectPuzzleSubmission = "CorrectPuzzleSubmission";
    public static readonly StringHash32 DocumentInspectStart = "DocumentInspectStart";
    public static readonly StringHash32 DocumentInspectEnd = "DocumentInspectEnd";
    public static readonly StringHash32 DocumentPuzzlePromptStart = "DocumentPuzzlePromptStart";
    public static readonly StringHash32 CutsceneBegin = "CutsceneBegin";
    public static readonly StringHash32 CutsceneEnd = "CutsceneEnd";
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
    public static readonly string HistoricalTemperature = "H. Temperature";
    public static readonly string HistoricalDistance = "H. Distance";
    public static readonly string HistoricalColor = "H. Color";
}