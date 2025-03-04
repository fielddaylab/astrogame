using Astro;
using FieldDay;
using FieldDay.Processes;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class NavProjectionState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public bool Initialized = false;
        public GameObject CelestialObjPrefab;
        public Sprite StarOutlineSprite;
        public Sprite PlanetOutlineSprite;
        
        public Canvas NavigationCanvas;
        public RectTransform OutlineGroup;

        public void OnRegister() {
            Game.Scenes.QueueOnLoad(() => {
                SpaceCameraState spaceCamState = Find.State<SpaceCameraState>();
                NavProjectionState navProjState = Find.State<NavProjectionState>();

                navProjState.NavigationCanvas.worldCamera = spaceCamState.Camera.Camera; 
                navProjState.NavigationCanvas.planeDistance = 700; 

                navProjState.NavigationCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            });

            Game.Events.Register(GameEvents.StartPuzzleNavigation, () => {
                NavigationCanvas.gameObject.SetActive(true);
                Initialized = false;
            });
            Game.Events.Register(GameEvents.StopPuzzleNavigation, () => {
                NavigationCanvas.gameObject.SetActive(false);
            });
        }

        public void OnDeregister() {}
    }
}