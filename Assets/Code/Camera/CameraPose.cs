using UnityEngine;
using FieldDay.Components;
using BeauRoutine.Splines;
using BeauUtil;
using System;

namespace Astro {
    public sealed class CameraPose : BatchedComponent {
        public float FieldOfView = 60;
        [NonSerialized] public Transform CachedTransform;
    }
}