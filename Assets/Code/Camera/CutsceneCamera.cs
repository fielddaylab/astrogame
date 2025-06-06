using FieldDay.Components;
using ScriptableBake;
using UnityEngine;
using UnityEngine.EventSystems;
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
                foreach(var raycaster in camera.GetComponents<BaseRaycaster>()) {
                    Baking.Destroy(raycaster);
                }
                Baking.Destroy(camera.GetComponent<UniversalAdditionalCameraData>());
                Baking.Destroy(camera);
            }
            return true;
        }

#endif // UNITY_EDITOR
    }

    static public class CutsceneUtility {
        static public void SyncCamera(CutsceneCamera camera, ViewState view) {
            camera.Tracker.GetPositionAndRotation(out Vector3 pos, out Quaternion rot);
            view.Camera.RootTransform.SetPositionAndRotation(pos, rot);
            view.Camera.EffectsTransform.SetLocalPositionAndRotation(default, default);
        }
    }
}