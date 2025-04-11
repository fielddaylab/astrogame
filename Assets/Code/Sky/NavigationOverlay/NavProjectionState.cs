using BeauUtil;
using BeauUtil.UI;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using System;
using UnityEngine;
using UnityEngine.UI;
using static Astro.PuzzleAsset;

namespace Astro {
    public class NavProjectionState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Initialized = false;
        // public Sprite StarOutlineSprite;

        public Color NavigationCompleteColor;
        
        public Canvas NavigationCanvas;
        public RectTransform NavigationArrow;
        public RectTransform OutlineGroup;
        public CanvasGroup BoarderGroup;

        public Sprite NeutrinoReticleElbow;
        public Sprite ConstellationReticleElbow;

        public void OnRegister() {
            Game.Scenes.QueueOnLoad(() => {
                SpaceCameraState spaceCamState = Find.State<SpaceCameraState>();
                NavProjectionState navProjState = Find.State<NavProjectionState>();

                navProjState.NavigationCanvas.worldCamera = spaceCamState.Camera.Camera; 
                navProjState.NavigationCanvas.planeDistance = 700; 

                navProjState.NavigationCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            });

            Game.Events.Register(GameEvents.StartPuzzleNavigation, NavigationCanvasUtil.SetupConstellationNavUI);
            Game.Events.Register(GameEvents.StopPuzzleMode, NavigationCanvasUtil.DisableConstellationNavUI);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartPuzzleNavigation, NavigationCanvasUtil.SetupConstellationNavUI);
            Game.Events.Deregister(GameEvents.StopPuzzleMode, NavigationCanvasUtil.DisableConstellationNavUI);
        }
    }

    static public partial class NavigationCanvasUtil {
        public static void SetupConstellationNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.NavigationArrow.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
            state.OutlineGroup.gameObject.SetActive(true);

            // Set approrpiate elbow sprites
            foreach (Image image in state.BoarderGroup.transform.GetComponentsInChildren<Image>()) {
                image.sprite = state.ConstellationReticleElbow;
            }
            state.BoarderGroup.gameObject.SetActive(true);
            state.Initialized = false;

            NavigationCanvasUtil.InitNavProjectionSystem(state);
        }

        public static void DisableConstellationNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.OutlineGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(false);
        }

        public static void SetupNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.OutlineGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
            state.NavigationArrow.gameObject.SetActive(false);

            // Set approrpiate elbow sprites
            foreach (Image image in state.BoarderGroup.transform.GetComponentsInChildren<Image>()) {
                image.sprite = state.NeutrinoReticleElbow;
            }
            state.BoarderGroup.gameObject.SetActive(true);
        }

        public static void DisableActiveNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(true);
        }

        public static void DisableNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            state.OutlineGroup.gameObject.SetActive(false);
            state.BoarderGroup.gameObject.SetActive(false);
            state.NavigationArrow.gameObject.SetActive(false);
            state.NavigationCanvas.gameObject.SetActive(false);
        }

        public static void UpdatedNeutrinoNavigationArrow(SpaceCameraState spaceCameraState) {
            NavProjectionState navProjectionState = Find.State<NavProjectionState>();
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            if (!config) return;

            SkyDome dome = Find.State<SkyDome>();

            EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;

            Vector3 targetPos = CelestialPositionerUtility.GetObjectPosition(dome.Position, target.RightAscension, target.Declination);

            Transform navCanvas = navProjectionState.NavigationCanvas.transform;
            RectTransform navArrow = navProjectionState.NavigationArrow;
            
            Vector3 viewPoint = spaceCameraState.Camera.Camera.WorldToViewportPoint(targetPos);

            bool isTargetVisible = viewPoint.z > 0;
            Vector2 direction = new Vector2(viewPoint.x - 0.5f, viewPoint.y - 0.5f);

            if (!isTargetVisible) direction = - direction;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            // Set the arrow's rotation
            navArrow.localEulerAngles = new Vector3(0, 0, angle - 90); // -90 because the arrow points up by default

            if (!IsTargetOnScreen(viewPoint)) {
                direction.Normalize();
                navArrow.gameObject.SetActive(true);
                
                if (!Find.State<NeutrinoNavigationState>().NavigationModeActive) ScriptUtility.Trigger(ScriptEvents.OnLeaveNeutrinoRegion);
                 
                // Position the arrow within the screen bounds
                Vector2 canvasSize = navCanvas.GetComponent<RectTransform>().sizeDelta / 2;

                // Calculate the offset from center
                Vector2 offset = direction * canvasSize;

                offset.x = Mathf.Clamp(offset.x, -canvasSize.x + navArrow.rect.width, canvasSize.x - navArrow.rect.width);
                offset.y = Mathf.Clamp(offset.y, -canvasSize.y + navArrow.rect.height, canvasSize.y - navArrow.rect.height);
                
                // Set the arrow's position
                navArrow.anchoredPosition = offset;
            } else {
                navArrow.gameObject.SetActive(false);
            }
        }

        private static bool IsTargetOnScreen(Vector3 viewportPosition) {
            return viewportPosition.x > 0 
            && viewportPosition.x < 1 
            && viewportPosition.y > 0 
            && viewportPosition.y < 1 
            && viewportPosition.z > 0;
        }

        public static void InitNavProjectionSystem(NavProjectionState navState)
        {
            var dome = Find.State<SkyDome>();
            var focusPools = Find.State<FocusPools>();
            var outlineState = Find.State<OutlineState>();

            var puzzleState = Find.State<PuzzleState>();
            var spaceCam = Find.State<SpaceCameraState>();

            if (!puzzleState.ActivePuzzle) return;

            EqCoords target = puzzleState.ActivePuzzle.PuzzleCoordinates;
            Vector3 spaceCameraOriginalRot = spaceCam.Camera.RootTransform.localEulerAngles;

            // populate sky with celestial objects
            var center = dome.Position;

            Camera spaceCamera = spaceCam.Camera.Camera;

            // Hack: just point our camera at our target position to draw our navigation overlay
            float stashedFOV = spaceCamera.fieldOfView;
            WorldPositionUtility.LookAt(spaceCam, target);
            spaceCamera.fieldOfView = spaceCam.Camera.OriginalFOV / puzzleState.ActivePuzzle.PuzzleCameraZoom;

            // Remove any old projections
            for (int i = 0; i < navState.OutlineGroup.childCount; i++)
            {
                GameObject.Destroy(navState.OutlineGroup.GetChild(i).gameObject);
            }

            for (int i = 0; i < puzzleState.ActivePuzzle.Rows.Length; i++)
            {
                CelestialAsset currAsset = Find.NamedAsset<CelestialAsset>(puzzleState.ActivePuzzle.Rows[i].Object);
                Vector3 assetPostion = CelestialPositionerUtility.GetObjectPosition(center, currAsset.Coords.RightAscension, currAsset.Coords.Declination);

                // Use UIFocus pool
                var navFocus = focusPools.Focii.Alloc(navState.OutlineGroup);
                navFocus.name = currAsset.DisplayName + " (Navigation Outline)";
                // TODO we can remove sprite representations on the nav ui if we dont have outlines
                InitNavRepresntation(navFocus, currAsset, null);

                outlineState.ActiveOutlines.PushBack(navFocus);

                Vector2 viewPoint = spaceCamera.WorldToViewportPoint(assetPostion);
                navFocus.Rect.anchorMin = navFocus.Rect.anchorMax = viewPoint;
            }

            // Create connection for constellations
            for (int i = 0; i < puzzleState.ActivePuzzle.Edges.Length; i++)
            {
                Edge e = puzzleState.ActivePuzzle.Edges[i];
                CelestialAsset ca1 = Find.NamedAsset<CelestialAsset>(e.Object1);
                CelestialAsset ca2 = Find.NamedAsset<CelestialAsset>(e.Object2);

                UIFocus focusA = outlineState.ActiveOutlines.Find(x => x.TargetData == ca1);
                UIFocus focusB = outlineState.ActiveOutlines.Find(x => x.TargetData == ca2);

                var connection = new GameObject(ca1.DisplayName + "_to_" + ca2.DisplayName, typeof(RectTransform));
                connection.transform.SetParent(navState.OutlineGroup, false);
                connection.AddComponent<RoundedRectGraphic>().color = new Color(0.9490196f, 1, 0.9411765f);
                RectTransform connectionRect = connection.GetComponent<RectTransform>();

                Vector2 canvasSize = navState.NavigationCanvas.GetComponent<RectTransform>().sizeDelta;
                float focusARadius = focusA.Rect.sizeDelta.x / 2;
                float focusBRadius = focusB.Rect.sizeDelta.x / 2;
                connectionRect.sizeDelta = new Vector2(5, Vector2.Distance(focusA.Rect.anchorMin * canvasSize, focusB.Rect.anchorMin * canvasSize) - focusARadius - focusBRadius);
                connectionRect.sizeDelta = new Vector2(5, Vector2.Distance(focusA.Rect.anchorMin * canvasSize, focusB.Rect.anchorMin * canvasSize) - 75);

                connectionRect.anchorMin = connectionRect.anchorMax = (focusA.Rect.anchorMax + focusB.Rect.anchorMax) / 2;
                Vector2 vector = (focusA.Rect.anchorMax * canvasSize) - (focusB.Rect.anchorMin * canvasSize);
                float angle = Vector2.Angle(Vector2.up, vector);
                connectionRect.localEulerAngles = new Vector3(0, 0, angle);
            }

            // Okay now put the camera back
            WorldPositionUtility.ForceLocalRotation(spaceCam, spaceCameraOriginalRot);
            navState.Initialized = true;
        }

        public static void InitNavRepresntation(UIFocus focus, CelestialAsset asset, Sprite represent2D) {
            focus.Represent2D.sprite = represent2D;
            if (represent2D == null)
            {
                focus.Represent2D.enabled = false;
            }
            // float scaleFactor = 1.5f * Mathf.Pow(0.63f, asset.ApparentMagnitude);
            // focus.Rect.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            focus.Represent2D.enabled = false;
            focus.TargetData = asset;
        }
    }
}