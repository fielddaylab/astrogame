using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class SkyDome : SharedStateComponent {
        public Transform StarRoot;

        [HideInInspector] public Vector3 Position;
        public float Radius = 1000;

        [NonSerialized] public CelestialObject[] AllObjects;
        [NonSerialized] public EqCoords Rotation;

        private void Awake() {
            Position = transform.position;
        }
    }
}
