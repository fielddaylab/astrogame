using Astro;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using BeauUtil;
using System;

public sealed class PuzzleNavigationState : SharedStateComponent {
    public ReviewModule ReviewModule;

    [NonSerialized] public bool NavigationModeActive = false;
    [NonSerialized] public float CameraDistanceFromPuzzle = -1;

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

        Debug.Log("[PuzzleNavUtil] Target:" + targetFoward +  ", Camera:" + spaceCamForward + " Distance:" + Vector3.Dot(targetFoward, spaceCamForward));
        float newDist = Vector3.Dot(targetFoward, spaceCamForward);

        if (newDist < 0) {
            SetPipReadout(puzzleNavState.ReviewModule, 0);
        } else if (newDist > 0 && newDist < 0.5) {
            SetPipReadout(puzzleNavState.ReviewModule, 1);
        } else if (newDist > 0.5 && newDist < 0.8) {
            SetPipReadout(puzzleNavState.ReviewModule, 2);
        } else if (newDist > 0.8 && newDist < 0.98) {
            SetPipReadout(puzzleNavState.ReviewModule, 3);
        } else if (newDist > 0.9999) {
            // ShowResultSprite(true);
            // Game.Events.Dispatch(GameEvents.PuzzleNavigationComplete);
        }

        puzzleNavState.CameraDistanceFromPuzzle = newDist;
    }

    private static void SetPipReadout(ReviewModule module, int numPips) {
        module.PipsRevealed = numPips;

        for (int i = 0; i < module.CountdownSprites.Length; i++) { 
            if (i < numPips){ 
                module.CountdownSprites[i].enabled = true;
            }else{
                module.CountdownSprites[i].enabled = false;
            }
        }

        // module.ResultSprite.enabled = false;
    }

    // private static void ShowResultSprite(bool correct) {
    //     PlayerPointsState state = Find.State<PlayerPointsState>();

    //     if (correct) {
    //         state.ReviewModule.ResultSprite.sprite = state.PipCorrect;
    //     } else {
    //         state.ReviewModule.ResultSprite.sprite = state.PipIncorrect;
    //     }
    //     state.ReviewModule.ResultSprite.enabled = true;
    // }

    // private static void ResetReview(ReviewModule module) {
    //     module.PipsRevealed = 0;
    //     foreach (SpriteRenderer pip in module.CountdownSprites) { 
    //         pip.enabled = false;
    //     }
    //     module.ResultSprite.enabled = false;
    // }
}