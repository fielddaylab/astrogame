using System;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(LabInteractable))]
    public sealed class LabButton : BatchedComponent {
        public Transform Movable;
        public Vector3 LocalDisplacement;

        [NonSerialized] public Vector3 OriginalDisplacement;
    }
}