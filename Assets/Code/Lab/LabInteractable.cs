using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    public sealed class LabInteractable : BatchedComponent {
        [HideInInspector] public bool InteractReceived = false;
        public ViewNode ConnectedViewNode;
    }
}