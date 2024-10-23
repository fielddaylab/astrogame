using BeauUtil;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    /// <summary>
    /// The camera responsible for capturing the view of space from the telescope / naked eye
    /// </summary>
    public sealed class SpaceCameraRig : SharedStateComponent {
        [Header("Components")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera;
        [Required(ComponentLookupDirection.Children)] public CameraFOVPlane FOVPlane;
        public Transform RootTransform;
        public Transform EffectsTransform;
    }
}