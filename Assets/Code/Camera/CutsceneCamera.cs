using FieldDay.Components;
using ScriptableBake;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Astro {
    public sealed class CutsceneCamera : BatchedComponent, IBaked {
        public Transform Tracker;

#if UNITY_EDITOR

        int IBaked.Order { get { return 10000; } }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            var camera = GetComponentInChildren<Camera>();
            if (camera != null) {
                Tracker = camera.transform;
                Baking.Destroy(camera.GetComponent<UniversalAdditionalCameraData>());
                Baking.Destroy(camera);
            }
            return true;
        }

#endif // UNITY_EDITOR
    }
}