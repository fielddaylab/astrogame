using System;
using System.Collections;
using System.Collections.Generic;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(CelestialPositioner))]
    public sealed class CelestialObject : BatchedComponent {
        [NonSerialized] public CelestialAsset Resource;
    }
}
