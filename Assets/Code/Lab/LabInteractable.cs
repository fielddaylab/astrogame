using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    public sealed class LabInteractable : BatchedComponent {
        public bool IsDraggable = false;
        [HideInInspector] public bool InteractReceived = false;
        [HideInInspector] public bool InteractEnded = false;
        [HideInInspector] public bool IsDragging = false;
        [Space(10)]
        public bool MaintainExistingView = false;
        public ViewNode ConnectedViewNode;
    }
}