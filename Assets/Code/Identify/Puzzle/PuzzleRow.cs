using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class PuzzleRow : BatchedComponent
    {
        public DataSlot DataSlot;

        // TEMP TESTING
        public MeshRenderer[] Cells;
        public Material DefaultMat;
        public Material SelectedMat;
    }

    public static partial class PuzzleUtility
    {
        public static void UpdateRowVisuals(PuzzleRow row, bool selected)
        {
            for (int i = 0; i < row.Cells.Length; i++)
            {
                var mats = row.Cells[i].sharedMaterials;
                mats[0] = selected ? row.SelectedMat : row.DefaultMat;
                row.Cells[i].sharedMaterials = mats;
            }
        }
    }
}