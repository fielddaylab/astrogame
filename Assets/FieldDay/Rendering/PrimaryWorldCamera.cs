using UnityEngine;

namespace FieldDay.Rendering {
    /// <summary>
    /// Sets the given camera as the primary world camera.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-10000)]
    public sealed class PrimaryWorldCamera : MonoBehaviour {
        private void OnEnable() {
            Game.Rendering.SetPrimaryCamera(GetComponent<Camera>());
        }

        private void OnDisable() {
            Game.Rendering?.RemovePrimaryCamera(GetComponent<Camera>());
        }
    }
}