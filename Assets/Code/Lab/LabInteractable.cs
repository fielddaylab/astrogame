using System;
using FieldDay.Components;
using FieldDay.HID;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(CursorHint))]
    public sealed class LabInteractable : BatchedComponent {
        public bool IsDraggable = false;
        [HideInInspector] public bool InteractReceived = false;
        [HideInInspector] public bool InteractEnded = false;
        [HideInInspector] public bool IsDragging = false;
        [Space(10)]
        public bool MaintainExistingView = false;
        public ViewNode ConnectedViewNode;
        [NonSerialized] public CursorHint Cursor;

        private void Awake() {
            Cursor = GetComponent<CursorHint>();
        }
    }
}