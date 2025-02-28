using Astro;
using FieldDay;
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