using FieldDay.Components;
using UnityEngine;

namespace Astro.Reference {
    [RequireComponent(typeof(RefGuideControlAnim))]
    public sealed class RefGuideControlPulseAnim : BatchedComponent {
        public SpriteRenderer Renderer;
        public Color MinColor;
        public Color MaxColor;
    }
}