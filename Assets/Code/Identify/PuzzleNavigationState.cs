using Astro;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using BeauUtil;
using System;
using FieldDay.Debugging;
using BeauUtil.Debugger;
using System.Reflection;
using FieldDay.Rendering;

public sealed class PuzzleNavigationState : SharedStateComponent, IRegistrationCallbacks {
    [NonSerialized] public bool NavigationModeActive = false;
    [NonSerialized] public float CameraDistanceFromPuzzle = -1;
    [NonSerialized] public bool ReadoutDirty = false;

    public void OnRegister() {
        Game.Events.Register(GameEvents.StartPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStart);
        Game.Events.Register(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStopped);
    }

    public void OnDeregister() {
        Game.Events.Deregister(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStopped);
    } 
}

public static class PuzzleNavigationUtility {
    public static void OnPuzzleNavStart() {
        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Register(UpdateCameraDistanceFromPuzzle);

        PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>();
        puzzleNavState.NavigationModeActive = true;

        ViewNavUtility.LeafMoveToNode("Monitor");
    } 

    public static void OnPuzzleNavStopped() {
        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Deregister(UpdateCameraDistanceFromPuzzle);

        PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>(); 
        puzzleNavState.NavigationModeActive = false;
        ResetReview();

        ViewNavUtility.LeafMoveToNode("Right");
    } 

    [DebugMenuFactory]
    private static DMInfo DebugStartPuzzleNav() {
        DMInfo info = new DMInfo("Events");
        info.AddButton("Start Navigation Mode", () => {
            Game.Events.Dispatch(GameEvents.StartPuzzleNavigation);
        });
        return info;
    }

    [DebugMenuFactory]
    private static DMInfo DebugStopPuzzleNav() {
        DMInfo info = new DMInfo("Events");
        info.AddButton("Stop Navigation Mode", () => {
            Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
        });
        return info;
    }

    private static void ResetReview() {
        PlayerPointsState ppState = Find.State<PlayerPointsState>();
        ReviewModule module = ppState.ReviewModule;

        module.PipsRevealed = 0;
        foreach (MeshRenderer pip in module.CountdownSprites) {
            pip.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
        }
        module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
        ppState.SubmittedObject = ppState.SubmittedPuzzle = false;
        ppState.ReviewTimer.Paused = false;
    }

    public static void UpdateCameraDistanceFromPuzzle(SpaceCameraState spaceCameraState) {
        PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>();
        PuzzleState puzzleState = Find.State<PuzzleState>();

        if (!puzzleState.ActivePuzzle || !puzzleNavState.NavigationModeActive) return;

        EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;
        Quaternion targetQuat = Quaternion.Euler(
            360 - (float)CoordinateUtility.DeclinationToDecimalDegrees( target.Declination ),
            360 - (float)CoordinateUtility.RAToDegrees( target.RightAscension ),
            0
        );
        Vector3 targetFoward = Geom.Forward(targetQuat);

        Quaternion spaceCameraQuat = spaceCameraState.Camera.RootTransform.rotation;

        Vector3 spaceCamForward = Geom.Forward(spaceCameraQuat);
        // Debug.Log("[PuzzleNavUtil] Camera RA:" + CoordinateUtility.DegreesToRA(360 - spaceCameraQuat.eulerAngles.y) + " D:" + CoordinateUtility.DecimalDegreesToDeclination(360 - spaceCameraQuat.eulerAngles.x));

        // Debug.Log("[PuzzleNavUtil] Target:" + targetFoward +  ", Camera:" + spaceCamForward + " Distance:" + Vector3.Dot(targetFoward, spaceCamForward));
        float newDist = Vector3.Dot(targetFoward, spaceCamForward);

        puzzleNavState.CameraDistanceFromPuzzle = newDist;
        puzzleNavState.ReadoutDirty = true;
    }
}