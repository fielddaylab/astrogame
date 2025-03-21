using FieldDay.Components;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public sealed class RadioToggleState : SharedStateComponent {
        public LabButton Button;
        public Material VisibleMaterial;
        public Material RadioMaterial;

        [NonSerialized] public bool CurrentState;
    }
}