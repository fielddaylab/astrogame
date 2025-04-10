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
            var mats = highlight.Mesh.sharedMaterials;
            mats[0] = active ? highlightState.SelectedInstrumentMat : highlight.OriginalMaterial;
            highlight.Mesh.sharedMaterials = mats;
        }
    }
}