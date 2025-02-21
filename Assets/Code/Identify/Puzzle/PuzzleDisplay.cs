using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class PuzzleDisplay : BatchedComponent
    {
        public PuzzleCell[] Cells;
        public PuzzleHeader[] Headers;
        public PuzzleHeader Clues;
        public int NumCols;
        public Transform CellAnchorPos;
        public Transform HeaderAnchorPos;
        public float RowSpacing;
        public float ColSpacing;
        public float BaseCellWidth;
        public LabInteractable SubmitButton;
    }

    public static partial class PuzzleUtility
    {
        static private Vector3 OFFSCREEN_POS = new Vector3(0, -200, 0);
        static private float OFFSCREEN_SPACING = 40;

        public static void LoadCells(PuzzleDisplay display, RingBuffer<PuzzleCell> cells, PuzzleHeader[] headers, int numCols)
        {
            display.SubmitButton.gameObject.SetActive(false);

            int numRows = cells.Count / numCols;
            display.Cells = new PuzzleCell[numRows * numCols];
            display.Headers = headers;
            display.NumCols = numCols;

            for (int r = 0; r < numRows; r++) {
                for (int c = 0; c < numCols; c++) {
                    var currCell = cells[r * numCols + c];
                    display.Cells[r * numCols + c] = currCell;
                }
            }
        }
        public static void LayoutCells(PuzzleDisplay display, PuzzleState state, PuzzlePools pools, List<DataTypeMask> types)
        {
            var colData = LookupColData(state.Library, types);

            // position and scale headers
            var cumulativePos = new Vector3(-display.BaseCellWidth, 0, 0);

            for (int c = 0; c < display.NumCols; c++)
            {
                var currHeader = display.Headers[c];
                currHeader.transform.SetParent(display.HeaderAnchorPos, false);

                // scale
                var currScale = currHeader.transform.lossyScale;
                var origTextScaleLocal = currHeader.Text.transform.localScale;
                var origTextScaleLossy = currHeader.Text.transform.lossyScale;
                currHeader.transform.SetScale(currScale * colData[c].Bundle.Dims, Axis.X);
                var scaleRatio = new Vector3(
                    origTextScaleLossy.x / currHeader.Text.transform.lossyScale.x,
                    origTextScaleLossy.y / currHeader.Text.transform.lossyScale.y,
                    origTextScaleLossy.z / currHeader.Text.transform.lossyScale.z
                    );
                currHeader.Text.transform.SetScale(origTextScaleLocal.x * scaleRatio, Axis.X);

                // pos
                // uniform spacing regardless of previous element scaling
                cumulativePos.x += (display.BaseCellWidth * colData[c].Bundle.Dims.x + display.ColSpacing) / 2.0f;
                currHeader.transform.localPosition = cumulativePos;
                cumulativePos.x += (display.BaseCellWidth * colData[c].Bundle.Dims.x + display.ColSpacing) / 2.0f;
            }

            PuzzlePoolUtility.ClearAllocations(pools);

            // position and scale cells
            int numRows = display.Cells.Length / display.NumCols;
            cumulativePos = Vector3.zero;
            for (int r = 0; r < numRows; r++) {
                cumulativePos.x = -display.BaseCellWidth;
                for (int c = 0; c < display.NumCols; c++) {
                    var currCell = display.Cells[r * display.NumCols + c];
                    currCell.transform.SetParent(display.CellAnchorPos, false);
                    currCell.MeshFilter.mesh = colData[c].Bundle.Mesh;
                    UnityEngine.Object.Destroy(currCell.Collider);
                    currCell.Collider = currCell.Mesh.gameObject.AddComponent<BoxCollider>();

                    // assign the appropriate atlas output
                    if (PuzzlePoolUtility.TryAllocateOnBundleType(pools, colData[c].Bundle.Type, out var id)) {
                        currCell.AtlasOutput.RegionId = id;
                    }

                    // scale the render displays
                    var displayScale = currCell.AtlasOutput.TargetRenderer.transform.localScale;
                    displayScale *= colData[c].Bundle.Dims;
                    currCell.AtlasOutput.TargetRenderer.transform.localScale = displayScale;

                    //pos
                    // uniform spacing regardless of previous element scaling
                    cumulativePos.x += (display.BaseCellWidth * colData[c].Bundle.Dims.x + display.ColSpacing) / 2.0f;
                    currCell.transform.localPosition = cumulativePos;
                    currCell.ContentContainer.transform.localPosition = OFFSCREEN_POS + cumulativePos * OFFSCREEN_SPACING;
                    cumulativePos.x += (display.BaseCellWidth * colData[c].Bundle.Dims.x + display.ColSpacing) / 2.0f;
                }
                cumulativePos.y += display.RowSpacing;
            }
        }

        public static void ClearCells(PuzzleDisplay display) {
            foreach (PuzzleCell cell in display.Cells) {
                DataUtility.TryClearData(cell.DataSlot);
            }
        }

        public static void ClearRows(PuzzleState state, BitArray rowsCorrectness) {
            if (state.ActivePuzzle.Rows.Length != rowsCorrectness.Length) {
                throw new ArgumentException("[PuzzleUtility] Row correctness length doesn't match number of rows!");
            } 
            for (int r = 0; r < rowsCorrectness.Length; r++) {
                if (!rowsCorrectness[r]) { // if row incorrect:
                    for (int c = 0; c < state.Display.NumCols; c++) { // go through each cell
                        DataUtility.TryClearData(state.Display.Cells[r * state.Display.NumCols + c].DataSlot);
                    }
                }
            }
        }

        private static PuzzleCellLibrary.AssembledCellData[] LookupColData(PuzzleCellLibrary library, List<DataTypeMask> types)
        {
            PuzzleCellLibrary.AssembledCellData[] newDims = new PuzzleCellLibrary.AssembledCellData[types.Count];
            for (int i = 0; i < types.Count; i++) {
                library.Lookup(types[i], out var assembledData);
                newDims[i] = assembledData;
            }

            return newDims;
        }
    }
}
