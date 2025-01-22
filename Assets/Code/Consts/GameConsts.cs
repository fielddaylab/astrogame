

using Astro;
using BeauUtil;

public static class GameEvents {
    public static readonly StringHash32 DocumentSelected = "input:document-selected";
    public static readonly StringHash32 MonitorEmptySpaceClicked = "input:monitor-empty-space-clicked";
    public static readonly StringHash32 OpenModeStart = "start-open-mode";
    public static readonly StringHash32 PuzzleModeStart = "start-puzzle-mode";
}

public static class ScriptEvents {
    public static readonly StringHash32 OpenModeStart = "OpenModeStart";
    public static readonly StringHash32 PuzzleModeStart = "PuzzleModeStart";
    public static readonly StringHash32 PointsUpdated = "PointsUpdated";
    public static readonly StringHash32 CorrectPuzzleSubmission = "CorrectPuzzleSubmission";
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