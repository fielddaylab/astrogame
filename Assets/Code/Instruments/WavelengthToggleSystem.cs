using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using FieldDay.Systems;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Astro {
    public sealed class WavelengthToggleSystem : ComponentSystemBehaviour<WavelengthToggleButton, LabButton, LabInteractable> {
        public override void ProcessWork(float deltaTime) {
            WavelengthToggleState state = Find.State<WavelengthToggleState>();

            foreach (var btn in m_Components) {
                if (btn.ComponentB.InteractReceived) {
                    if (!btn.ComponentA.IsToggle) {
                        // TODO: play failed toggle btn
                        return;
                    }
                    if (!state.AllowChanges) {
                        LabButtonUtility.SetClicked(btn.ComponentA, true);
                        break;
                    }

                    WavelengthToggleUtility.SetMask(state, btn.Primary.Mask);
                    return;
                }
            }
        }
    }

    static public class WavelengthToggleUtility {
        static public void SetMask(WavelengthToggleState state, CelestialObjectVisMask mask) {
            var cam = Find.State<SpaceCameraState>();
            var skyGen = Find.State<SkyGenerationState>();

            if (state.CurrentState == mask) {
                return;
            }

            AdjustToggleState(state.VisibleButton, CelestialObjectVisMask.Visible, mask, true);
            AdjustToggleState(state.BlueButton, CelestialObjectVisMask.Blue, mask, true);
            AdjustToggleState(state.InfraredButton, CelestialObjectVisMask.Infrared, mask, true);

            cam.Skybox.material = SelectMaterial(mask, state.VisibleMaterial, state.BlueMaterial, state.InfraredMaterial);
            skyGen.VisMask = mask;
            skyGen.IsDirty = true;
            cam.LookUpdatedThisFrame = true;

            state.CurrentState = mask;
            AstroGame.Events.Dispatch(GameEvents.MonitorSwitchedFilter, EvtArgs.Create(skyGen.VisMask));

            // Horizon materials
            var skyDome = Find.State<SkyDome>();
            skyDome.HorizonPlane.sharedMaterial = SelectMaterial(mask, state.HorizonPlaneVisibleMaterial, state.HorizonPlaneBlueMaterial, state.HorizonPlaneInfraredMaterial);
            skyDome.HorizonRing.sharedMaterial = SelectMaterial(mask, state.HorizonRingVisibleMaterial, state.HorizonRingBlueMaterial, state.HorizonRingInfraredMaterial);
            skyDome.HorizonGlow.sharedMaterial = SelectMaterial(mask, state.HorizonGlowVisibleMaterial, state.HorizonGlowBlueMaterial, state.HorizonGlowInfraredMaterial);
        }

        static private Material SelectMaterial(CelestialObjectVisMask mask, Material visible, Material blue, Material infrared) {
            switch(mask) {
                case CelestialObjectVisMask.Visible:
                    return visible;
                case CelestialObjectVisMask.Blue:
                    return blue;
                case CelestialObjectVisMask.Infrared:
                    return infrared;
                default:
                    throw new ArgumentOutOfRangeException("mask");
            }
        }

        static private void AdjustToggleState(LabButton button, CelestialObjectVisMask buttonMask, CelestialObjectVisMask selectedMask, bool playSfx) {
            if (buttonMask == selectedMask) {
                LabButtonUtility.SetDown(button, playSfx);
                button.Collider.enabled = false;

                // Update indicators
                Material[] indicatorMats = button.GetComponent<WavelengthToggleButton>().Indicator.materials;
                indicatorMats[1] = Find.State<WavelengthToggleState>().LitIndicatorMaterial;
                button.GetComponent<WavelengthToggleButton>().Indicator.materials = indicatorMats;

            } else {
                LabButtonUtility.SetUp(button, false);
                button.Collider.enabled = true;

                // Update indicators
                Material[] indicatorMats = button.GetComponent<WavelengthToggleButton>().Indicator.materials;
                indicatorMats[1] = Find.State<WavelengthToggleState>().UnlitIndicatorMaterial;
                button.GetComponent<WavelengthToggleButton>().Indicator.materials = indicatorMats;
            }
        }
    }
}