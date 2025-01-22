using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public class InputState : SharedStateComponent, IRegistrationCallbacks {
        public bool InputEnabled;
        [HideInInspector] public int ClickableLayerMask;       

        public void OnDeregister() {
        }

        public void OnRegister() {
            InputUtility.SetClickableMaskDefault(this);
        }
    }

    public static class InputUtility {
        public static void SetInputEnabled(bool enabled) {
            SpaceCameraUtility.SetCameraInputEnabled(enabled);
            if (enabled) {
                Game.Input.ResumeRaycasts();
            } else {
                Game.Input.PauseRaycasts();
            }
        }

        public static void SetClickableMaskDefault(InputState state) {
            state.ClickableLayerMask = LayerMask.GetMask("LabInteract", "DocumentInteract", "ReferenceInteract");
        }

        public static void SetClickableMaskTopLayer(InputState state) {
            state.ClickableLayerMask = LayerMask.GetMask("TopLayer");
        }

    }

}