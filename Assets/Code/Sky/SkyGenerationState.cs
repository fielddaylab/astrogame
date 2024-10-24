using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyGenerationState : SharedStateComponent
    {
        [HideInInspector] public bool Initialized = false;
        public GameObject CelestialObjPrefab;
    }
}