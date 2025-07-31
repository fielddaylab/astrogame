using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;
using FieldDay.Scripting;
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
        static public void SetMask(WavelengthToggleState state, CelestialObjectVisMask mask, bool forceReset = false) {
            var cam = Find.State<SpaceCameraState>();
            var skyGen = Find.State<SkyGenerationState>();

            if (state.CurrentState == mask && !forceReset) {
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
            
            Material horizonObjsMaterial = SelectMaterial(mask, state.HorizonObjsVisibleMaterial, state.HorizonObjsAltMaterial, state.HorizonObjsAltMaterial);
            if (state.HorizonObjsRenderers == null) {
                state.HorizonObjsRenderers = skyDome.HorizonObjsRoot.GetComponentsInChildren<SpriteRenderer>();
            }
            foreach (var renderer in state.HorizonObjsRenderers) {
                renderer.SetSharedMaterialAtIndex(0, horizonObjsMaterial);
            }
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
            WavelengthToggleState state = Find.State<WavelengthToggleState>();
            MeshRenderer toggleIndicator = button.GetComponent<WavelengthToggleButton>().Indicator;

            InstrumentInventoryState invState = Find.State<InstrumentInventoryState>();
            LabInstrument wavelengthInstrument = null;
            bool isButtonActive = false;
            switch(selectedMask) {
                case CelestialObjectVisMask.Visible:
                    wavelengthInstrument = ScriptUtility.FindActor("VisibleWavelength").GetComponent<LabInstrument>();
                    isButtonActive = invState.ActiveInstruments.Contains(wavelengthInstrument);
                    break;
                case CelestialObjectVisMask.Blue:
                    wavelengthInstrument = ScriptUtility.FindActor("BlueWavelength").GetComponent<LabInstrument>();
                    isButtonActive = invState.ActiveInstruments.Contains(wavelengthInstrument);
                    break;
                case CelestialObjectVisMask.Infrared:
                    wavelengthInstrument = ScriptUtility.FindActor("InfraredWavelength").GetComponent<LabInstrument>();
                    isButtonActive = invState.ActiveInstruments.Contains(wavelengthInstrument);
                    break; 
            }


            if (buttonMask == selectedMask && isButtonActive) {
                LabButtonUtility.SetDown(button, playSfx);
                button.Collider.enabled = false;

                // Update indicators
                toggleIndicator.SetSharedMaterialAtIndex(1, state.LitIndicatorMaterial);
            } else {
                LabButtonUtility.SetUp(button, false);
                button.Collider.enabled = true;

                // Update indicators
                toggleIndicator.SetSharedMaterialAtIndex(1, state.UnlitIndicatorMaterial);
            }
        }
    }
}