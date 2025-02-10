using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyGenerationState : SharedStateComponent
    {
        [NonSerialized] public bool Initialized = false;
        public GameObject CelestialObjPrefab;
        public Sprite DefaultStarSprite;
        public Sprite DefaultPlanetSprite;
    }
}