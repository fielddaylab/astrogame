using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class NavProjectionState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Initialized = false;
        public Sprite StarOutlineSprite;

        public Color NavigationCompleteColor;
        
        public Canvas NavigationCanvas;
        public RectTransform NavigationArrow;
        public RectTransform OutlineGroup;
        public CanvasGroup BoarderGroup;

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
            state.BoarderGroup.gameObject.SetActive(true);
            state.Initialized = false;
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
            state.NavigationArrow.gameObject.SetActive(true);
            state.BoarderGroup.gameObject.SetActive(true);
        }

        public static void DisableNeutrinoNavUI() {
            NavProjectionState state = Find.State<NavProjectionState>();

            state.BoarderGroup.gameObject.SetActive(false);
            // state.NavigationArrow.gameObject.SetActive(true);
            state.NavigationCanvas.gameObject.SetActive(true);
        }

        public static void UpdatedNeutrinoNavigationArrow(SpaceCameraState spaceCameraState) {
            NavProjectionState navProjectionState = Find.State<NavProjectionState>();
            DayConfigAsset config = DayConfigUtil.GetConfigForState();
            if (!config) return;


            SkyDome dome = Find.State<SkyDome>();

            EqCoords target = config.NeutrinoEvent.NeutrinoCoordinates;

            Vector3 targetPos = CelestialPositionerUtility.GetObjectPosition(dome.Position, target.RightAscension, target.Declination);
            Quaternion spaceCameraQuat = spaceCameraState.Camera.RootTransform.rotation;
            Vector3 cameraCenterPos = CelestialPositionerUtility.GetObjectPosition(
                dome.Position, 
                CoordinateUtility.DegreesToRA(360 - spaceCameraQuat.eulerAngles.y), 
                CoordinateUtility.DecimalDegreesToDeclination(360 - spaceCameraQuat.eulerAngles.x) 
                );
            Vector3 targetVector = targetPos - spaceCameraState.Camera.RootTransform.position;
            Debug.DrawLine(dome.Position, targetPos, Color.cyan, 100f);
            Debug.DrawLine(dome.Position, cameraCenterPos, Color.blue, 100f);
            Debug.DrawLine(cameraCenterPos, targetPos, Color.green, 100f);
            Vector3 direction = targetVector - cameraCenterPos;

            Transform navCanvas = navProjectionState.NavigationCanvas.transform;
            Vector3 canvasProjection = Vector3.ProjectOnPlane(direction, navCanvas.forward); 
            // Set Navigation Arrow Position

            float angle = Vector2.Angle(navCanvas.up, canvasProjection);
            RectTransform navArrow = navProjectionState.NavigationArrow;
            navArrow.localEulerAngles = new Vector3(0f, 0f, -angle);
            
            // Vector2 viewPoint = spaceCameraState.Camera.Camera.WorldToViewportPoint(targetPos);
            // Debug.Log("[NavProjectionState] raw viewPoint:" + viewPoint);
            // viewPoint.x = Mathf.Clamp(viewPoint.x, 0, 1);
            // viewPoint.y = Mathf.Clamp(viewPoint.y, 0, 1);

            // Debug.Log("[NavProjectionState] clamped viewPoint:" + viewPoint);
            // navArrow.anchorMin = navArrow.anchorMax = viewPoint;
        }
    }
}