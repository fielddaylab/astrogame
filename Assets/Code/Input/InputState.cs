using System;
using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Astro {
    public class InputState : SharedStateComponent, IRegistrationCallbacks {
        public bool InputEnabled = true;
        public PhysicsRaycaster Raycaster;

        [NonSerialized] public int DesiredLayerMask;
        [NonSerialized] public int LayerMaskFilter = Bits.All32;
        [NonSerialized] public int AppliedLayerMask;

        public void OnDeregister() {
        }

        public void OnRegister() {
            InputUtility.SetClickableMaskDefault(this);
        }
    }

    public static class InputUtility {
        public const int DefaultLayerMask = LayerMasks.LabInteract_Mask | LayerMasks.DocumentInteract_Mask | LayerMasks.ReferenceInteract_Mask | LayerMasks.InstrumentInteract_Mask;

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
            state.DesiredLayerMask = DefaultLayerMask;
            state.AppliedLayerMask = state.DesiredLayerMask & state.LayerMaskFilter;
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskCustom(InputState state, LayerMask mask) {
            state.DesiredLayerMask = mask;
            state.AppliedLayerMask = state.DesiredLayerMask & state.LayerMaskFilter;
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskTopLayer(InputState state) {
            state.DesiredLayerMask = LayerMasks.TopLayer_Mask;
            state.AppliedLayerMask = LayerMasks.TopLayer_Mask | (state.DesiredLayerMask & state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskFilter(InputState state, LayerMask filter) {
            state.LayerMaskFilter = filter;
            state.AppliedLayerMask = (LayerMasks.TopLayer_Mask & state.DesiredLayerMask) | (state.DesiredLayerMask & state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

    }

}