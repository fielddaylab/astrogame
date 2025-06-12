using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using BeauUtil;
using System;
using BeauRoutine;
using FieldDay.Debugging;
using BeauUtil.Debugger;
using System.Collections;
using BeauPools;
using BeauUtil.UI;

namespace Astro {
    public enum NavigationMode {
        Neutrino,
        Constellation,
        Inactive
    }

    public sealed class NavigationState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public NavigationMode CurrentNavigationMode = NavigationMode.Inactive;
        [NonSerialized] public float CameraDistanceFromTarget = -1;
        [NonSerialized] public Vector3 CameraForward = Vector3.zero;
        [NonSerialized] public Routine ConstellationSnapRoutine;
        [NonSerialized] public bool ReadoutDirty = false;
        [NonSerialized] public bool ResultShown = false;

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartNeutrinoNavigation, NavigationCanvasUtil.SetupNeutrinoNavUI);
            Game.Events.Register(GameEvents.StartNeutrinoNavigation, NavigationUtility.OnNeutrinoNavStart);
            Game.Events.Register(GameEvents.StopNeutrinoNavigation, NavigationCanvasUtil.DisableActiveNeutrinoNavUI);
            Game.Events.Register(GameEvents.StopNeutrinoNavigation, NavigationUtility.OnNeutrinoNavStopped);
            Game.Events.Register(GameEvents.StopOpenMode, NavigationUtility.OnOpenIdStopped);

            Game.Events.Register(GameEvents.StartPuzzleNavigation, NavigationUtility.OnPuzzleNavStart);
            Game.Events.Register(GameEvents.PuzzleNavigationComplete, NavigationUtility.OnPuzzleNavComplete);
            Game.Events.Register(GameEvents.StopPuzzleNavigation, NavigationUtility.OnPuzzleNavStopped);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartNeutrinoNavigation, NavigationCanvasUtil.SetupNeutrinoNavUI);
            Game.Events.Deregister(GameEvents.StartNeutrinoNavigation, NavigationUtility.OnNeutrinoNavStart);
            Game.Events.Deregister(GameEvents.StopNeutrinoNavigation, NavigationCanvasUtil.DisableActiveNeutrinoNavUI);
            Game.Events.Deregister(GameEvents.StopNeutrinoNavigation, NavigationUtility.OnNeutrinoNavStopped);
            Game.Events.Deregister(GameEvents.StopOpenMode, NavigationUtility.OnOpenIdStopped);

            Game.Events.Deregister(GameEvents.StopPuzzleNavigation, NavigationUtility.OnPuzzleNavStart);
            Game.Events.Deregister(GameEvents.StopPuzzleNavigation, NavigationUtility.OnPuzzleNavStopped);
        } 
    }

    public static class NavigationUtility {
        public static void OnPuzzleNavStart() {
            SpaceCameraState state = Find.State<SpaceCameraState>();
            state.OnLookUpdated.Register(UpdateCameraDistanceFromPuzzle);

            NavigationState navState = Find.State<NavigationState>();
            navState.CurrentNavigationMode = NavigationMode.Constellation;
            navState.CameraDistanceFromTarget = -1;

            ViewNavUtility.LeafMoveToNode("Monitor");

            WavelengthToggleState wavelengthState = Find.State<WavelengthToggleState>();
            wavelengthState.AllowChanges = false;
            WavelengthToggleUtility.SetMask(wavelengthState, CelestialObjectVisMask.Visible);
        } 

        public static void OnPuzzleNavComplete() {
            NavigationState navState = Find.State<NavigationState>(); 
            Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
            navState.ConstellationSnapRoutine = Routine.Null;
        }

        public static void OnPuzzleNavStopped() {
            SpaceCameraState state = Find.State<SpaceCameraState>();
            state.OnLookUpdated.Deregister(UpdateCameraDistanceFromPuzzle);

            NavigationState navState = Find.State<NavigationState>(); 
            navState.CurrentNavigationMode = NavigationMode.Inactive;
            navState.ReadoutDirty = false;
            ReviewModuleUtility.ResetReview();

            ViewNavUtility.LeafMoveToNode("Right");

            WavelengthToggleState wavelengthState = Find.State<WavelengthToggleState>();
            wavelengthState.AllowChanges = true;
        }

        public static void OnNeutrinoNavStart() {
            SpaceCameraState state = Find.State<SpaceCameraState>();
            state.OnLookUpdated.Register(UpdateCameraDistanceFromPuzzle);

            NavigationState navState = Find.State<NavigationState>();
            navState.CurrentNavigationMode = NavigationMode.Neutrino;
            navState.CameraDistanceFromTarget = -1;
            navState.ResultShown = false;

            ViewNavUtility.LeafMoveToNode("Monitor");
            state.LookUpdatedThisFrame = true;
            state.OnLookUpdated.Invoke(state);

            WavelengthToggleState wavelengthState = Find.State<WavelengthToggleState>();
            wavelengthState.AllowChanges = false;
            WavelengthToggleUtility.SetMask(wavelengthState, CelestialObjectVisMask.Visible);
        }

        public static void OnNeutrinoNavStopped() {
            NavigationState navState = Find.State<NavigationState>();
            navState.CurrentNavigationMode = NavigationMode.Inactive;
            navState.ReadoutDirty = false;

            SpaceCameraState state = Find.State<SpaceCameraState>();
            state.OnLookUpdated.Register(NavigationCanvasUtil.UpdatedNeutrinoNavigationArrow);
            state.OnLookUpdated.Deregister(UpdateCameraDistanceFromPuzzle);

            state.LookUpdatedThisFrame = true;
            state.OnLookUpdated.Invoke(state);

            ViewNavUtility.LeafMoveToNode("Right");

            WavelengthToggleState wavelengthState = Find.State<WavelengthToggleState>();
            wavelengthState.AllowChanges = true;
        }

        public static void OnOpenIdStopped() { 
            SpaceCameraState state = Find.State<SpaceCameraState>();
            state.OnLookUpdated.Deregister(NavigationCanvasUtil.UpdatedNeutrinoNavigationArrow);

            ReviewModuleUtility.ResetReview(Find.State<ReviewState>().ReviewModule);
            NavigationCanvasUtil.DisableNeutrinoNavUI();
        }

        [DebugMenuFactory]
        private static DMInfo DebugNeutrinoNav() {
            DMInfo info = new DMInfo("Events");
            info.AddButton("Start Neutrino Navigation", () => {
                Game.Events.Dispatch(GameEvents.StartNeutrinoNavigation);
            });
            info.AddButton("Stop Neutrino Navigation", () => {
                Game.Events.Dispatch(GameEvents.StopNeutrinoNavigation);
            });
            return info;
        }

        [DebugMenuFactory]
        private static DMInfo DebugOpenId() {
            DMInfo info = new DMInfo("Events");
            info.AddButton("Start OpenId", () => {
                Game.Events.Dispatch(GameEvents.StartOpenMode);
            });
            info.AddButton("Stop OpenId", () => {
                Game.Events.Dispatch(GameEvents.StopOpenMode);
            });
            return info;
        }

        [DebugMenuFactory]
        private static DMInfo DebugPuzzleNav() {
            DMInfo info = new DMInfo("Events");
            info.AddButton("Start Puzzle Navigation", () => {
                Game.Events.Dispatch(GameEvents.StartPuzzleNavigation);
            });
            info.AddButton("Stop Puzzle Navigation", () => {
                Game.Events.Dispatch(GameEvents.StopPuzzleNavigation);
            });
            return info;
        }

        public static void UpdateCameraDistanceFromPuzzle(SpaceCameraState spaceCameraState) { 
            NavigationState navState = Find.State<NavigationState>();

            EqCoords target = new EqCoords();
            if (navState.CurrentNavigationMode == NavigationMode.Neutrino) {
                // Target comes from the neutrino origin for this day
                DayConfigAsset config = DayConfigUtil.GetConfigForState();
                if (!config) return;

                target = config.NeutrinoEvent.NeutrinoCoordinates;
            } else if (navState.CurrentNavigationMode == NavigationMode.Constellation) {
                // Target comes from the active puzzle for this day
                PuzzleState puzzleState = Find.State<PuzzleState>();
                if (!puzzleState.ActivePuzzle) return;

                target = puzzleState.ActivePuzzle.PuzzleCoordinates;
            }

            Vector3 targetFoward = WorldPositionUtility.GetLookVector(target);

            Quaternion spaceCameraQuat = spaceCameraState.Camera.RootTransform.rotation;

            Vector3 spaceCamForward = Geom.Forward(spaceCameraQuat);
            float newDist = Vector3.Dot(targetFoward, spaceCamForward);

            navState.CameraForward = spaceCamForward;
            navState.CameraDistanceFromTarget = newDist;
            navState.ReadoutDirty = true;
        }

        public static IEnumerator SnapAlignment(EqCoords targetCoords) {
            InputState inputState = Find.State<InputState>();
            SpaceCameraState spaceCameraState = Find.State<SpaceCameraState>();
            NavProjectionState navProjectionState = Find.State<NavProjectionState>();

            bool inputCache = inputState.InputEnabled;
            InputUtility.SetInputEnabled(inputState, false);

            Quaternion targetQuat = WorldPositionUtility.GetLocalLookRotation(spaceCameraState, targetCoords);

            CanvasGroup boarder = navProjectionState.BoarderGroup;
            CanvasGroup outline = navProjectionState.OutlineGroup.GetComponent<CanvasGroup>();
            CanvasGroup puzzleOutline = navProjectionState.PuzzleOutlineGroup.GetComponent<CanvasGroup>();
            yield return spaceCameraState.Camera.RootTransform.RotateQuaternionTo(targetQuat, 0.5f, Space.Self).Ease(Curve.Smooth).OnUpdate(OnCameraAutomaticallyRotated);
            spaceCameraState.OnLookUpdated.Invoke(spaceCameraState);
            yield return Routine.Combine(
                Tween.Value(1f, 0.04f, (f) => { outline.alpha = f; }, Mathf.Lerp, 0.4f),
                Tween.Value(1f, 0.2f, (f) => { puzzleOutline.alpha = f; }, Mathf.Lerp, 0.4f),
                Tween.Value(boarder.alpha, 0f, (f) => { boarder.alpha = f; }, Mathf.Lerp, 0.4f),
                Tween.Color(navProjectionState.ConstellationEdgeColor, navProjectionState.NavigationCompleteColor, (c) => { UpdateEdgeGroupColor(navProjectionState.PuzzleOutlineGroup, c); }, 0.4f, ColorUpdate.FullColor)
            ); 

            boarder.gameObject.SetActive(false);
            boarder.alpha = 1.0f;

            InputUtility.SetInputEnabled(inputState, inputCache);
        }

        private static void OnCameraAutomaticallyRotated(float _) {
            var state = Find.State<SpaceCameraState>();
            WorldPositionUtility.UpdateLookCoordinatesFromCurrentLook(state);
        }

        // Helper for updating the edge color of constellations in camera snap routine
        private static void UpdateEdgeGroupColor(RectTransform OutlineGroup, Color color) {
            using (PooledList<RoundedRectGraphic> list = PooledList<RoundedRectGraphic>.Create()) {
                OutlineGroup.GetComponentsInChildren<RoundedRectGraphic>(list);
                foreach (RoundedRectGraphic graphic in list) {
                    graphic.color = color;
                }
            }
        }
    }
}