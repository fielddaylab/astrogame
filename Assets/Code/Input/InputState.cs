using System;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Astro {
    public class InputState : SharedStateComponent, IRegistrationCallbacks {
        public bool InputEnabled = true;
        public PhysicsRaycaster Raycaster;
        [NonSerialized] public int ClickableLayerMask;

        public void OnDeregister() {
        }

        public void OnRegister() {
            InputUtility.SetClickableMaskDefault(this);
        }
    }

    public static class InputUtility {
        public static void SetInputEnabled(InputState state, bool enabled) {
            state.InputEnabled = enabled;
            SpaceCameraUtility.SetCameraInputEnabled(enabled);
            if (enabled) {
                Game.Input.ResumeRaycasts();
            } else {
                Game.Input.PauseRaycasts();
            }
        }

        public static void SetClickableMaskDefault(InputState state) {
            state.ClickableLayerMask = LayerMasks.LabInteract_Mask | LayerMasks.DocumentInteract_Mask | LayerMasks.ReferenceInteract_Mask;
            state.Raycaster.eventMask = state.ClickableLayerMask;
        }

        public static void SetClickableMaskTopLayer(InputState state) {
            state.ClickableLayerMask = LayerMasks.TopLayer_Mask;
            state.Raycaster.eventMask = state.ClickableLayerMask;
        }

    }

}