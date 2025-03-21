using Astro;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using BeauUtil;
using BeauRoutine;
using System;
using FieldDay.Debugging;
using BeauUtil.Debugger;
using FieldDay.Rendering;
using System.Collections;
using UnityEngine.UI;

public sealed class PuzzleNavigationState : SharedStateComponent, IRegistrationCallbacks {
    [NonSerialized] public bool NavigationModeActive = false;
    [NonSerialized] public float CameraDistanceFromPuzzle = -1;
    [NonSerialized] public bool ReadoutDirty = false;
    [NonSerialized] public Routine ConstellationSnapRoutine;

    public void OnRegister() {
        Game.Events.Register(GameEvents.StartPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStart);
        Game.Events.Register(GameEvents.PuzzleNavigationComplete, PuzzleNavigationUtility.OnPuzzleNavComplete);
        Game.Events.Register(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStopped);
    }

    public void OnDeregister() {
        Game.Events.Deregister(GameEvents.StopPuzzleNavigation, PuzzleNavigationUtility.OnPuzzleNavStart);
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

    public static void OnPuzzleNavComplete() {
        PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>(); 
        Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
        puzzleNavState.ConstellationSnapRoutine = Routine.Null;
    }

    public static void OnPuzzleNavStopped() {
        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Deregister(UpdateCameraDistanceFromPuzzle);

        PuzzleNavigationState puzzleNavState = Find.State<PuzzleNavigationState>(); 
        puzzleNavState.NavigationModeActive = false;
        puzzleNavState.ReadoutDirty = false;
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
        Vector3 targetFoward = WorldPositionUtility.GetLookVector(target);

        Quaternion spaceCameraQuat = spaceCameraState.Camera.RootTransform.rotation;

        Vector3 spaceCamForward = Geom.Forward(spaceCameraQuat);

        // Debug.Log("[PuzzleNavUtil] Target:" + targetFoward +  ", Camera:" + spaceCamForward + " Distance:" + Vector3.Dot(targetFoward, spaceCamForward));
        float newDist = Vector3.Dot(targetFoward, spaceCamForward);

        puzzleNavState.CameraDistanceFromPuzzle = newDist;
        puzzleNavState.ReadoutDirty = true;
    }

    public static IEnumerator SnapConstellationAlignment() {
        InputState inputState = Find.State<InputState>();
        SpaceCameraState spaceCameraState = Find.State<SpaceCameraState>();
        NavProjectionState navProjectionState = Find.State<NavProjectionState>();

        bool inputCache = inputState.InputEnabled;
        InputUtility.SetInputEnabled(inputState, false);

        PuzzleState puzzleState = Find.State<PuzzleState>();

        EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;
        Quaternion targetQuat = WorldPositionUtility.GetLookRotation(spaceCameraState, target);

        CanvasGroup boarder = navProjectionState.BoarderGroup;
        CanvasGroup outline = navProjectionState.OutlineGroup.GetComponent<CanvasGroup>();
        yield return spaceCameraState.Camera.RootTransform.RotateQuaternionTo(targetQuat, 0.5f).Ease(Curve.Smooth).OnUpdate((_) => spaceCameraState.LookUpdatedThisFrame = true);
        // TODO: Figure out how to make the FocusVisuals update throughout the rotation tween
        spaceCameraState.OnLookUpdated.Invoke(spaceCameraState);
        yield return Routine.Combine(
            Tween.Value(1f, 0.2f, (f) => { outline.alpha = f; }, Mathf.Lerp, 0.4f),
            Tween.Value(boarder.alpha, 0f, (f) => { boarder.alpha = f; }, Mathf.Lerp, 0.4f),
            Tween.Color(Color.white, navProjectionState.NavigationCompleteColor, (c) => { UpdateEdgeGroupColor(navProjectionState.OutlineGroup, c); }, 0.4f, ColorUpdate.FullColor)
        ); 

        boarder.gameObject.SetActive(false);
        boarder.alpha = 1.0f;

        InputUtility.SetInputEnabled(inputState, inputCache);
        Game.Events.Dispatch(GameEvents.PuzzleNavigationComplete);
    }

    // Helper for updating the edge color of constellations in camera snap routine
    private static void UpdateEdgeGroupColor(RectTransform OutlineGroup, Color color) {
        Image[] edgeImages = OutlineGroup.GetComponentsInChildren<Image>();
        for(int i = 0; i < edgeImages.Length; i++) {
            edgeImages[i].color = color;
        }
    }
}