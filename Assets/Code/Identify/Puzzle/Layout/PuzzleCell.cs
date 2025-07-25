using FieldDay.Components;
using FieldDay.Rendering;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class PuzzleCell : BatchedComponent {
        public DataSlot DataSlot;
        public MeshRenderer MainMesh;
        public MeshFilter MainMeshFilter;
        public MeshRenderer OutlineMesh;
        public MeshFilter OutlineMeshFilter;
        public BoxCollider Collider;
        public RenderAtlasOutput AtlasOutput;
        public Camera Camera;
        public Transform ContentContainer;
    }

    public static partial class PuzzleUtility {
        public static void UpdateCellVisuals(PuzzleState state, PuzzleCell cell, bool selected) {
            cell.OutlineMesh.SetSharedMaterialAtIndex(0, selected ? state.SelectedCellMat : state.UnselectedCellMat);
        }
    }
}