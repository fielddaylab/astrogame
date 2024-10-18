using UnityEngine;

namespace FieldDay.UI {
    /// <summary>
    /// Sets the given camera as the primary gui camera.
    /// </summary>
    [RequireComponent(typeof(Camera)), DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class PrimaryGuiCamera : MonoBehaviour {
        private void OnEnable() {
            Game.Gui.SetPrimaryCamera(GetComponent<Camera>());
        }

        private void OnDisable() {
            Game.Gui?.RemovePrimaryCamera(GetComponent<Camera>());
        }
    }
}