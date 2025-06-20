using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(ViewNode))]
    public sealed class ViewFreeLook : BatchedComponent {
        [Header("Range")]
        public float HorizontalRange;
        public float VerticalRange;

        [Header("Cursor Tuning")]
        public float DeadZone = 0.2f;
        public float Edge = 0.8f;

        [Header("Movement Speed")]
        public float LerpStrength = 10;

        [Header("Scaling")]
        public float DefaultScale = 1;
        public float CutsceneScale = 1;
    }
}