using UnityEngine;
using FieldDay.Components;
using BeauRoutine.Splines;
using BeauUtil;
using System;

namespace Astro {
    public sealed class CameraPose : BatchedComponent {
        public CameraFOVMode Mode;
        [HideIfField("IsFOVDirect")] public Transform Target = null;
        [HideIfField("IsFOVDirect")] public float Height = 10;
        [HideIfField("IsFOVDirect")] public float Zoom = 1;
        [ShowIfField("IsFOVDirect")] public float FieldOfView = 30;

        [NonSerialized] public Transform CachedTransform;

#if UNITY_EDITOR
        private bool IsFOVDirect() {
            return Mode == CameraFOVMode.Direct;
        }
#endif // UNITY_EDITOR
    }
}