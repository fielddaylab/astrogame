using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Astro {
    public sealed class RadioToggleSystem : SharedStateSystemBehaviour<RadioToggleState> {
        public override void ProcessWork(float deltaTime) {
            if (!m_State.Button.CachedInteractable.InteractReceived) { return; }

            RadioToggleUtility.SetRadioToggle(m_State, !m_State.CurrentState);
        }
    }

    static public class RadioToggleUtility {
        static public void SetRadioToggle(RadioToggleState state, bool toggle) {
            var cam = Find.State<SpaceCameraState>();
            var skyGen = Find.State<SkyGenerationState>();

            state.CurrentState = toggle;
            cam.Skybox.material = toggle ? state.RadioMaterial : state.VisibleMaterial;
            skyGen.VisMask = toggle ? CelestialObjectVisMask.Radio : CelestialObjectVisMask.Visible;
            skyGen.IsDirty = true;
            cam.LookUpdatedThisFrame = true;

            AstroGame.Events.Dispatch(GameEvents.MonitorSwitchedFilter, EvtArgs.Create(skyGen.VisMask));

            FocusableUtility.SetCurrentFocus(Find.State<FocusState>(), null);

            // Horizon materials
            var skyDome = Find.State<SkyDome>();
            skyDome.HorizonPlane.material = toggle ? state.HorizonPlaneRadioMaterial : state.HorizonPlaneVisibleMaterial;
            skyDome.HorizonRing.material = toggle ? state.HorizonRingRadioMaterial : state.HorizonRingVisibleMaterial;
            skyDome.HorizonGlow.material = toggle ? state.HorizonGlowRadioMaterial : state.HorizonGlowVisibleMaterial;
        }
    }
}