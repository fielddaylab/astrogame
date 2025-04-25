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

public sealed class NeutrinoNavigationState : SharedStateComponent, IRegistrationCallbacks {
    [NonSerialized] public bool NavigationModeActive = false;
    [NonSerialized] public float CameraDistanceFromOrigin = -1;
    [NonSerialized] public Vector3 CameraForward = Vector3.zero;
    [NonSerialized] public bool ReadoutDirty = false;
    [NonSerialized] public bool ResultShown = false;

    public void OnRegister() {
        Game.Events.Register(GameEvents.StartNeutrinoNavigation, NavigationCanvasUtil.SetupNeutrinoNavUI);
        Game.Events.Register(GameEvents.StartNeutrinoNavigation, NeutrinoNavigationUtility.OnNeutrinoNavStart);
        Game.Events.Register(GameEvents.StopNeutrinoNavigation, NavigationCanvasUtil.DisableActiveNeutrinoNavUI);
        Game.Events.Register(GameEvents.StopNeutrinoNavigation, NeutrinoNavigationUtility.OnNeutrinoNavStopped);
        Game.Events.Register(GameEvents.StopOpenMode, NeutrinoNavigationUtility.OnOpenIdStopped);
    }

    public void OnDeregister() {
        Game.Events.Deregister(GameEvents.StartNeutrinoNavigation, NavigationCanvasUtil.SetupNeutrinoNavUI);
        Game.Events.Deregister(GameEvents.StartNeutrinoNavigation, NeutrinoNavigationUtility.OnNeutrinoNavStart);
        Game.Events.Deregister(GameEvents.StopNeutrinoNavigation, NavigationCanvasUtil.DisableActiveNeutrinoNavUI);
        Game.Events.Deregister(GameEvents.StopNeutrinoNavigation, NeutrinoNavigationUtility.OnNeutrinoNavStopped);
        Game.Events.Deregister(GameEvents.StopOpenMode, NeutrinoNavigationUtility.OnOpenIdStopped);
    } 
}

public static class NeutrinoNavigationUtility {
    public static void OnNeutrinoNavStart() {
        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Register(UpdateCameraDistanceFromPuzzle);

        NeutrinoNavigationState navState = Find.State<NeutrinoNavigationState>();
        navState.NavigationModeActive = true;
        navState.ResultShown = false;

        ViewNavUtility.LeafMoveToNode("Monitor");
        state.LookUpdatedThisFrame = true;
        state.OnLookUpdated.Invoke(state);
    }

    public static void OnNeutrinoNavStopped() {
        NeutrinoNavigationState navState = Find.State<NeutrinoNavigationState>();
        navState.NavigationModeActive = false;
        navState.ReadoutDirty = false;

        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Register(NavigationCanvasUtil.UpdatedNeutrinoNavigationArrow);
        state.OnLookUpdated.Deregister(UpdateCameraDistanceFromPuzzle);

        state.LookUpdatedThisFrame = true;
        state.OnLookUpdated.Invoke(state);

        ViewNavUtility.LeafMoveToNode("Desk");
    }

    public static void OnOpenIdStopped() { 
        SpaceCameraState state = Find.State<SpaceCameraState>();
        state.OnLookUpdated.Deregister(NavigationCanvasUtil.UpdatedNeutrinoNavigationArrow);

        ReviewModuleUtility.ResetReview(Find.State<ReviewState>().ReviewModule);
        NavigationCanvasUtil.DisableNeutrinoNavUI();
    }

    public static void UpdateCameraDistanceFromPuzzle(SpaceCameraState spaceCameraState) {
        NeutrinoNavigationState navState = Find.State<NeutrinoNavigationState>();
        DayConfigAsset config = DayConfigUtil.GetConfigForState();

        if (!config) return;

        EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;
        Vector3 targetFoward = WorldPositionUtility.GetLookVector(target);

        Quaternion spaceCameraQuat = spaceCameraState.Camera.RootTransform.rotation;

        Vector3 spaceCamForward = Geom.Forward(spaceCameraQuat);
        float newDist = Vector3.Dot(targetFoward, spaceCamForward);

        navState.CameraForward = spaceCamForward;
        navState.CameraDistanceFromOrigin = newDist;
        navState.ReadoutDirty = true;
    }
}