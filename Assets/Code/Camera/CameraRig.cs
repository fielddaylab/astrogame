using BeauUtil;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class CameraRig : SharedStateComponent {
        [Header("Components")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera;
        [Required(ComponentLookupDirection.Children)] public CameraFOVPlane FOVPlane;
        public Transform RootTransform;
        public Transform EffectsTransform;
    }
}