using Astro;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using BeauUtil;
using System;

public sealed class PuzzleNavigationState : SharedStateComponent {
    //[NonSerialized] 
    public bool NavigationModeActive = true;
    [NonSerialized] public float CameraDistanceFromPuzzle = -1;
    [NonSerialized] public bool ReadoutDirty = false;

    // public void OnRegister() {
    //     Game.Events.Register(GameEvents.StartPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStart);
    //     Game.Events.Register(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStopped);
    // }

    // public void OnDeregister() {
    //     Game.Events.Deregister(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStopped);
    // } 
}

public static class PuzzleNavigationUtility {
    [InvokeOnBoot]
    static public void Init() {
        SpaceCameraState state = Find.State<SpaceCameraState>();

        state.OnLookUpdated.Register(UpdateCameraDistanceFromPuzzle);
    }

    // public static void OnPuzzleNavStart() {
    //     PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>();
    //     puzzleNavState.NavigationModeActive = true;

    // } 

    // public static void OnPuzzleNavStopped() {
    //     PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>();
    //     puzzleNavState.NavigationModeActive = false;

    //     PuzzleNavigationUtility.ResetReview(puzzleNavState.ReviewModule);
    // } 

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