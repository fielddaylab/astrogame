using BeauUtil;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyGenerationState : SharedStateComponent
    {
        public Sprite DefaultStarSprite;
        public Sprite DefaultPlanetSprite;

        [NonSerialized] public CelestialObjectVisMask VisMask = CelestialObjectVisMask.Visible;
        [NonSerialized] public bool IsDirty = true;
    }
}