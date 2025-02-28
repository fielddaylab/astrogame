using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Astro.FocusPools;

namespace Astro {
    public class NavProjectionState : SharedStateComponent {
        [NonSerialized] public bool Initialized = false;
        public GameObject CelestialObjPrefab;
        public Sprite StarOutlineSprite;
        public Sprite PlanetOutlineSprite;
        
        public Canvas NavigationCanvas;
    }
}