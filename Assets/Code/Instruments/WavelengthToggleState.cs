using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astro {
    public sealed class WavelengthToggleState : SharedStateComponent, IRegistrationCallbacks {
        public LabButton VisibleButton;
        public LabButton BlueButton;
        [FormerlySerializedAs("RadioButton")] public LabButton InfraredButton;

        [Header("Monitor")]
        public Material VisibleMaterial;
        public Material BlueMaterial;
        [FormerlySerializedAs("RadioMaterial")] public Material InfraredMaterial;
 
        public Material LitIndicatorMaterial;
        public Material UnlitIndicatorMaterial;

        [Header("Horizon")]
        public Material HorizonPlaneVisibleMaterial;
        public Material HorizonPlaneBlueMaterial;
        [FormerlySerializedAs("HorizonPlaneRadioMaterial")] public Material HorizonPlaneInfraredMaterial;
        public Material HorizonRingVisibleMaterial;
        public Material HorizonRingBlueMaterial;
        [FormerlySerializedAs("HorizonRingRadioMaterial")] public Material HorizonRingInfraredMaterial;
        public Material HorizonGlowVisibleMaterial;
        public Material HorizonGlowBlueMaterial;
        [FormerlySerializedAs("HorizonGlowRadioMaterial")] public Material HorizonGlowInfraredMaterial;
        public Material HorizonObjsVisibleMaterial;
        public Material HorizonObjsAltMaterial;

        [NonSerialized] public CelestialObjectVisMask CurrentState = CelestialObjectVisMask.Visible;

        [NonSerialized] public bool AllowChanges = true;

        [NonSerialized] public SpriteRenderer[] HorizonObjsRenderers;

        public void OnDeregister() {
        }

        public void OnRegister() {
            LabButtonUtility.ForceDown(VisibleButton);
            LabButtonUtility.ForceUp(BlueButton);
            LabButtonUtility.ForceUp(InfraredButton);

            VisibleButton.GetComponent<Collider>().enabled = false;
        }
    }
}