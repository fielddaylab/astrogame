using System;
using FieldDay.Components;
using FieldDay.HID;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(CursorHint))]
    public sealed class LabInteractable : BatchedComponent {
        public bool IsDraggable = false;
        [NonSerialized] public bool InteractReceived = false;
        [NonSerialized] public bool InteractEnded = false;
        [NonSerialized] public bool IsDragging = false;
        [NonSerialized] public CursorHint Cursor;

        private void Awake() {
            Cursor = GetComponent<CursorHint>();
        }
    }
}