using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class PuzzleCell : BatchedComponent
    {
        public DataSlot DataSlot;
        public MeshRenderer Mesh;
        public MeshFilter MeshFilter;
        public BoxCollider Collider;
        public RenderAtlasOutput AtlasOutput;
        public Transform ContentContainer;
    }

    public static partial class PuzzleUtility
    {
        public static void UpdateCellVisuals(PuzzleState state, PuzzleCell cell, bool selected)
        {
            var mats = cell.Mesh.sharedMaterials;
            mats[0] = selected ? state.SelectedCellMat : state.UnselectedCellMat;
            cell.Mesh.sharedMaterials = mats;
        }
    }
}