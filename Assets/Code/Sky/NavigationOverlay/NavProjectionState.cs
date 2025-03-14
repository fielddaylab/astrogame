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

    static public class NavigationCanvasUtil {
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

            state.NavigationArrow.gameObject.SetActive(true);
            state.NavigationCanvas.gameObject.SetActive(true);
        }

    }
}