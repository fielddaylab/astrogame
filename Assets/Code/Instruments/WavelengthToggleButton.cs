using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(LabButton))]
    public sealed class WavelengthToggleButton : BatchedComponent {
        public CelestialObjectVisMask Mask = CelestialObjectVisMask.Visible;
        public MeshRenderer Indicator;
    }
}