using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Audio;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(LabInteractable))]
    public sealed class LabButton : BatchedComponent {
        public enum State {
            Up,
            Down,
            Click
        }
        
        public Transform Movable;
        public Vector3 LocalDisplacement;
        public bool IsToggle;
        public bool AutoToggle;

        [Header("Sounds")]
        [AudioEvent] public StringHash32 ClickSfx;
        [AudioEvent] public StringHash32 ToggleSfx;
        [AudioEvent] public StringHash32 UntoggleSfx;

        [NonSerialized] public Collider Collider;
        [NonSerialized] public LabInteractable CachedInteractable;
        [NonSerialized] public Vector3 OriginalDisplacement;
        [NonSerialized] public State CurrentState = State.Up;
        [NonSerialized] public Routine TransitionRoutine;

        private void Awake() {
            OriginalDisplacement = Movable.localPosition;
            this.CacheComponent(ref CachedInteractable);
            this.CacheComponent(ref Collider);
        }
    }
}