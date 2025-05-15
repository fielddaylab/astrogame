using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.Components;
using TMPro;
using System;

namespace Astro
{
    public class RelevantSlotHighlight : BatchedComponent
    {
        public MeshRenderer Mesh;
        [NonSerialized] public Material OriginalMaterial;
        [NonSerialized] public bool Dimmed;
        [NonSerialized] public bool Ignored;

        private void Awake() {
            if (Mesh) {
                OriginalMaterial = Mesh.sharedMaterials[0];
            }
        }
    }

    public static class SlotHighlightUtility
    {
        static public void SetAvailableHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active)
        {
            var mats = highlight.Mesh.sharedMaterials;
            mats[0] = active ? highlightState.AvailableCellMat : highlightState.UnselectedCellMat;
            highlight.Mesh.sharedMaterials = mats;
        }

        static public void SetSelectedHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active)
        {
            var mats = highlight.Mesh.sharedMaterials;
            mats[0] = active ? highlightState.SelectedCellMat : highlightState.UnselectedCellMat;
            highlight.Mesh.sharedMaterials = mats;
        }

        static public void SetInstrumentHighlight(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool active) {
            if (highlight.Dimmed) { return; }
            var mats = highlight.Mesh.sharedMaterials;
            mats[0] = active ? highlightState.SelectedInstrumentMat : highlight.OriginalMaterial;
            highlight.Mesh.sharedMaterials = mats;
        }

        static public void SetInstrumentDimmed(SlotHighlightState highlightState, RelevantSlotHighlight highlight, bool dimmed) {
            var mats = highlight.Mesh.sharedMaterials;
            mats[0] = dimmed ? highlightState.DimmedInstrumentMat : highlight.OriginalMaterial;
            highlight.Mesh.sharedMaterials = mats;
            highlight.Dimmed = dimmed;
        }

        public static void SetInstrumentButtonsDimmed(InstrumentInventoryState state, bool dim) {
            SlotHighlightState highlightState = Find.State<SlotHighlightState>();
            foreach (LabInstrument instrument in state.ActiveInstruments) {
                foreach (DataSlot slot in instrument.AutoPopulated) {
                    if (slot.IsHidingData) {
                        continue;
                    }

                    if (slot.TryGetComponent(out RelevantSlotHighlight effect)) {
                        SetInstrumentDimmed(highlightState, effect, dim);
                    }
                }
            }
        }
    }
}