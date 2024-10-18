using System.Collections;
using System.Collections.Generic;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class SkyDome : SharedStateComponent {
        public Vector3 Position;
        public float Radius = 1000;

        private void Awake() {
            Position = transform.position;
        }
    }
}
