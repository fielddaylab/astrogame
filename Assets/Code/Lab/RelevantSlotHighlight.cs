using System;
using UnityEngine;
using FieldDay;
using FieldDay.Components;
using FieldDay.Rendering;

namespace Astro {
    public class RelevantSlotHighlight : BatchedComponent {
        public MeshRenderer Mesh;
        public bool Ignored;

        [Tooltip("If not set, this will default to the first material in the MeshRenderer if available")]
        [NonSerialized] public Material OriginalMaterial;
        [NonSerialized] public bool Dimmed;

        private void Awake() {
            if (!Mesh) return;

            if (!TryGetComponent<InteractSelectPuzzleCell>(out var i)) {
                Dimmed = true;
            }

            // Assumed to be the dimmed material if this needs to be unlocked
            if (OriginalMaterial == null) OriginalMaterial = Mesh.sharedMaterials[0];
        }
    }

    public static class SlotHighlightUtility
    {
        static public void SetAvailableHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active) {
            highlight.Mesh.SetSharedMaterialAtIndex(0, active ? highlightState.AvailableCellMat : highlightState.UnselectedCellMat);
        }

        static public void SetSelectedHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active) {
            highlight.Mesh.SetSharedMaterialAtIndex(0, active ? highlightState.SelectedCellMat : highlightState.UnselectedCellMat);
        }

        static public void SetInstrumentHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active) {
            if (highlight.Dimmed) return;

            highlight.Mesh.SetSharedMaterialAtIndex(0, active ? highlightState.SelectedInstrumentMat : highlight.OriginalMaterial);
        }

        static public void SetInstrumentDimmed(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool dimmed) {
            highlight.Mesh.SetSharedMaterialAtIndex(0, dimmed ? highlightState.DimmedInstrumentMat : highlight.OriginalMaterial);
            highlight.Dimmed = dimmed;
        }

        public static void SetInstrumentButtonsDimmed(InstrumentInventoryState state, bool dim) {
            SlotHighlightState highlightState = Find.State<SlotHighlightState>();
            foreach (LabInstrument instrument in state.ActiveInstruments) {
                foreach (DataSlot slot in instrument.AutoPopulated) {
                    if (!slot.gameObject.activeSelf || !slot.IsActive) {
                        continue;
                    }

                    if (slot.TryGetComponent(out RelevantSlotHighlight effect) && !effect.Ignored) {
                        SetInstrumentDimmed(highlightState, effect, dim);
                    }
                }
            }
        }
    }
}