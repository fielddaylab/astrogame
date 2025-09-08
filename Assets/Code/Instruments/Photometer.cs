using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Astro {
    [RequireComponent(typeof (LabInstrument))]
    public class Photometer : BatchedComponent, IRegistrationCallbacks {
        //public DataDisplay SharedDisplay;
        public DataSlot ApparentSlot;
        public DataSlot AbsoluteSlot;

        public DataSlot BlueSlot;
        public DataSlot IRSlot;

        public InteractTransferData TransferPort;

        [Header("Panel")]
        public Transform FlipPanel;
        public GameObject ApparentButton;
        public GameObject AbsoluteButton;

        [Header("Lights")]
        public MeshRenderer VisibleLight;
        public MeshRenderer BlueLight;
        public MeshRenderer IRLight;
        public Material LightLitMaterial;
        public Material LightUnlitMaterial;

        [Header("Connected")]
        public DataSlot[] DependentSlots;

        [NonSerialized] public bool IsAbsoluteModeOn;

        [NonSerialized] public Routine FlipRoutine;

        public void OnDeregister() {
            AstroGame.Events.DeregisterAllForContext(this);
        }

        public void OnRegister() {
            PhotometerUtility.TogglePhotometerMode(false, this);
            AstroGame.Events.Register<CelestialObjectVisMask>(GameEvents.MonitorSwitchedFilter, OnFilterSwitched)
                .Register<UIFocus>(GameEvents.OnStarSelected, OnStarSelected)
                .Register<StringHash32>(GameEvents.InstrumentUnlocked, OnInstrumentUnlocked);
        }

        private void OnFilterSwitched(CelestialObjectVisMask visibility) {
            PhotometerUtility.HandleFilterChanged(this, visibility, Find.State<FocusState>().CurrentFocus);
        }

        private void OnStarSelected(UIFocus focus) {
            PhotometerUtility.HandleStarSelected(this, Find.State<SkyGenerationState>().VisMask, focus);
        }

        private void OnInstrumentUnlocked(StringHash32 instrumentId) {
            PhotometerUtility.HandleInstrumentUnlocked(this, instrumentId);
        }
    }

    public static partial class PhotometerUtility {
        public static void TogglePhotometerMode(bool absoluteOn, Photometer photometer){
            bool wasDifferent = Ref.Replace(ref photometer.IsAbsoluteModeOn, absoluteOn);

            photometer.ApparentSlot.IsActive = !absoluteOn;
            photometer.AbsoluteSlot.IsActive = absoluteOn;

            photometer.ApparentButton.layer = absoluteOn ? LayerMasks.IgnoreRaycast_Index : LayerMasks.InstrumentInteract_Index;
            photometer.AbsoluteButton.layer = !absoluteOn ? LayerMasks.IgnoreRaycast_Index : LayerMasks.InstrumentInteract_Index;

            if (wasDifferent) {
                photometer.FlipRoutine.Replace(photometer, photometer.FlipPanel.RotateTo(absoluteOn ? 0 : -180, 0.35f, Axis.Z, Space.Self).Ease(Curve.CubeInOut));
            }

            //if (absoluteOn && photometer.TransferPort.DataSlot == photometer.ApparentSlot) {
                //DataUtility.Rewire(photometer.TransferPort, photometer.AbsoluteSlot);
            //} else if (!absoluteOn && photometer.TransferPort == photometer.AbsoluteSlot) {
            //    DataUtility.Rewire(photometer.TransferPort, photometer.ApparentSlot);
            //}
        }

        public static void HandleFilterChanged(Photometer photometer, CelestialObjectVisMask visibility, UIFocus focus) {
            Assert.True((visibility & (visibility - 1)) == 0, "filter must be set to single mask only");

            PlayerCelestialAssetKnowledgeFlags knownFlags = default;

            bool IsVisibleInNewFilter = true;
            if (focus != null) {
                IsVisibleInNewFilter = (focus.TargetData.Visibility & visibility) != 0;
            }

            switch (visibility) {
                case CelestialObjectVisMask.Blue: {
                    DataUtility.Rewire(photometer.TransferPort, photometer.BlueSlot);
                    if (!IsVisibleInNewFilter) break;

                    if (AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadBlueAppMag, photometer.BlueSlot, out knownFlags)) {
                        AttemptRevealDependentSlots(photometer, knownFlags);
                    }
                    break;
                }
                case CelestialObjectVisMask.Infrared: {
                    DataUtility.Rewire(photometer.TransferPort, photometer.IRSlot);
                    if (!IsVisibleInNewFilter) break;

                    AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadIRAppMag, photometer.IRSlot, out knownFlags);
                    break;
                }
                case CelestialObjectVisMask.Visible: {
                    DataUtility.Rewire(photometer.TransferPort, photometer.ApparentSlot);
                    if (!IsVisibleInNewFilter) break;

                    if (AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadVisibleAppMag, photometer.ApparentSlot, out knownFlags)) {
                        DataUtility.RevealData(photometer.AbsoluteSlot);
                        AttemptRevealDependentSlots(photometer, knownFlags);
                    }
                    break;
                }
            }

            photometer.VisibleLight.sharedMaterial = visibility == CelestialObjectVisMask.Visible ? photometer.LightLitMaterial : photometer.LightUnlitMaterial;
            photometer.BlueLight.sharedMaterial = visibility == CelestialObjectVisMask.Blue ? photometer.LightLitMaterial : photometer.LightUnlitMaterial;
            photometer.IRLight.sharedMaterial = visibility == CelestialObjectVisMask.Infrared ? photometer.LightLitMaterial : photometer.LightUnlitMaterial;
        }

        public static void HandleStarSelected(Photometer photometer, CelestialObjectVisMask visMask, UIFocus focus) {
            if (!focus) {
                return;
            }

            CelestialObjectVisMask visibility = Find.State<SkyGenerationState>().VisMask;
            Assert.True((visibility & (visibility - 1)) == 0, "filter must be set to single mask only");

            PlayerCelestialAssetKnowledgeFlags knownFlags = default;

            switch (visibility) {
                case CelestialObjectVisMask.Blue: {
                        if (AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadBlueAppMag, photometer.BlueSlot, out knownFlags)) {
                            AttemptRevealDependentSlots(photometer, knownFlags);
                        }
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadVisibleAppMag, knownFlags, photometer.ApparentSlot);
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadIRAppMag, knownFlags, photometer.IRSlot);
                        break;
                    }
                case CelestialObjectVisMask.Infrared: {
                        AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadIRAppMag, photometer.IRSlot, out knownFlags);
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadVisibleAppMag, knownFlags, photometer.ApparentSlot);
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadBlueAppMag, knownFlags, photometer.BlueSlot);
                        break;
                    }
                case CelestialObjectVisMask.Visible: {
                        if (AttemptRevealSlot(focus, visibility, PlayerCelestialAssetKnowledgeFlags.HasReadVisibleAppMag, photometer.ApparentSlot, out knownFlags)) {
                            DataUtility.RevealData(photometer.AbsoluteSlot);
                            AttemptRevealDependentSlots(photometer, knownFlags);
                        }
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadBlueAppMag, knownFlags, photometer.BlueSlot);
                        DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags.HasReadIRAppMag, knownFlags, photometer.IRSlot);
                        break;
                    }
            }

            if ((knownFlags & PlayerCelestialAssetKnowledgeFlags.HasReadBothVisibleAndBlue) != PlayerCelestialAssetKnowledgeFlags.HasReadBothVisibleAndBlue) {
                foreach(var slot in photometer.DependentSlots) {
                    DataUtility.HideData(slot);
                }
            } else {
                foreach (var slot in photometer.DependentSlots) {
                    DataUtility.RevealData(slot);
                }
            }

            if ((knownFlags & PlayerCelestialAssetKnowledgeFlags.HasReadVisibleAppMag) != 0) {
                DataUtility.RevealData(photometer.AbsoluteSlot);
            } else {
                DataUtility.HideData(photometer.AbsoluteSlot);
            }
        }

        public static void HandleInstrumentUnlocked(Photometer photometer, StringHash32 instrumentId) {
            if (instrumentId == "BlueWavelength") {
                foreach(var display in photometer.BlueSlot.Displays) {
                    display.DefaultOutput.enabled = true;
                }
            } else if (instrumentId == "InfraredWavelength") {
                foreach (var display in photometer.IRSlot.Displays) {
                    display.DefaultOutput.enabled = true;
                }
            }
        }

        public static bool AttemptRevealSlot(UIFocus selection, CelestialObjectVisMask visMask, PlayerCelestialAssetKnowledgeFlags flags, DataSlot slot, out PlayerCelestialAssetKnowledgeFlags knownFlags) {
            if (!selection) {
                knownFlags = default;
                return default;
            }

            PlayerKnowledgeQueryResult known = PlayerKnowledgeUtility.KnowsFlags(selection.TargetData.AssetId, flags, out var record);
            if (known == PlayerKnowledgeQueryResult.NotKnown) {
                record.Flags |= flags;
                knownFlags = record.Flags;
                DataUtility.RevealData(slot);
                PlayerKnowledgeUtility.UpdateKnowledge(selection.TargetData.AssetId, record);
                return true;
            }

            knownFlags = record.Flags;
            return false;
        }

        private static void DisplaySlotIfPresent(PlayerCelestialAssetKnowledgeFlags flags, PlayerCelestialAssetKnowledgeFlags knownFlags, DataSlot slot) {
            if ((knownFlags & flags) == flags) {
                DataUtility.RevealData(slot);
            } else {
                DataUtility.HideData(slot);
            }
        }

        private static void AttemptRevealDependentSlots(Photometer photometer, PlayerCelestialAssetKnowledgeFlags knownFlags) {
            if ((knownFlags & PlayerCelestialAssetKnowledgeFlags.HasReadBothVisibleAndBlue) != PlayerCelestialAssetKnowledgeFlags.HasReadBothVisibleAndBlue) {
                return;
            }

            foreach(var slot in photometer.DependentSlots) {
                DataUtility.RevealData(slot);
            }
        }
    }
}