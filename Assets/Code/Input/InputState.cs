using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.SharedState;
using FieldDay.UI;
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
            // Handles cases where we might be in the middle of a transition that we are going to stop early
            if (!InputEnabled) {
                InputUtility.SetInputEnabled(this, true);
            }
        }

        public void OnRegister() {
            InputUtility.SetClickableMaskDefault(this);
        }
    }

    public static class InputUtility {
        public const int DefaultLayerMask = LayerMasks.LabInteract_Mask | LayerMasks.DocumentInteract_Mask | LayerMasks.ReferenceInteract_Mask | LayerMasks.InstrumentInteract_Mask;

        public static void SetInputEnabled(InputState state, bool enabled) {
            Log.Msg("[InputState] called SetInputEnabled()");
            bool changed = Ref.Replace(ref state.InputEnabled, enabled);
            SpaceCameraUtility.SetCameraInputEnabled(enabled);
            if (!changed) return;

            if (enabled) {
                Log.Msg("[InputState > SetInputEnabled] ResumeRaycasts");
                Game.Input.ResumeRaycasts();
            } else {
                Log.Msg("[InputState > SetInputEnabled] PauseRaycasts");
                Game.Input.PauseRaycasts();
            }
        }

        public static void SetClickableMaskDefault(InputState state) {
            state.DesiredLayerMask = DefaultLayerMask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskCustom(InputState state, LayerMask mask) {
            state.DesiredLayerMask = mask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskTopLayer(InputState state) {
            state.DesiredLayerMask = LayerMasks.TopLayer_Mask;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        public static void SetClickableMaskFilter(InputState state, LayerMask filter) {
            state.LayerMaskFilter = filter;
            state.AppliedLayerMask = CalculateFinalMask(state.DesiredLayerMask, state.LayerMaskFilter);
            state.Raycaster.eventMask = state.AppliedLayerMask;
        }

        static private int CalculateFinalMask(int desiredMask, int filter) {
            return (LayerMasks.TopLayer_Mask & desiredMask) | (desiredMask & filter);
        }

        public static bool IsClickable(InputState state, GameObject gameObject) {
            return RaycastUtility.IsInteractableByRaycaster(gameObject, state.Raycaster);
        }
    }

}