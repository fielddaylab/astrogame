using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.Components;

namespace Astro
{
    public class RelevantSlotHighlight : BatchedComponent
    {
        public MeshRenderer Mesh;
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
    }
}