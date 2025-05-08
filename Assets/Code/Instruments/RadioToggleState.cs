using FieldDay.Components;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public sealed class RadioToggleState : SharedStateComponent {
        public LabButton Button;
        [Header("Monitor")]
        public Material VisibleMaterial;
        public Material RadioMaterial;

        [Header("Horizon")]
        public Material HorizonPlaneVisibleMaterial;
        public Material HorizonPlaneBlueMaterial;
        public Material HorizonPlaneRadioMaterial;
        public Material HorizonRingVisibleMaterial;
        public Material HorizonRingBlueMaterial;
        public Material HorizonRingRadioMaterial;
        public Material HorizonGlowVisibleMaterial;
        public Material HorizonGlowBlueMaterial;
        public Material HorizonGlowRadioMaterial;

        [NonSerialized] public bool CurrentState;
    }
}