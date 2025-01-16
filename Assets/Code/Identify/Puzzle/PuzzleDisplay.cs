using BeauRoutine;
using BeauUtil;
using FieldDay.Components;
using System.Collections.Generic;
using TMPro;
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
        public static void LayoutCells(PuzzleDisplay display, List<DataTypeMask> types)
        {
            var colDims = GenerateColDims(types);

            // position and scale headers
            var cumulativePos = new Vector3(-display.BaseCellWidth, 0, 0);

            for (int c = 0; c < display.NumCols; c++)
            {
                var currHeader = display.Headers[c];
                currHeader.transform.SetParent(display.HeaderAnchorPos);

                // scale
                var currScale = currHeader.transform.lossyScale;
                var origTextScaleLocal = currHeader.Text.transform.localScale;
                var origTextScaleLossy = currHeader.Text.transform.lossyScale;
                currHeader.transform.SetScale(currScale * colDims[c], Axis.X);
                var scaleRatio = new Vector3(
                    origTextScaleLossy.x / currHeader.Text.transform.lossyScale.x,
                    origTextScaleLossy.y / currHeader.Text.transform.lossyScale.y,
                    origTextScaleLossy.z / currHeader.Text.transform.lossyScale.z
                    );
                currHeader.Text.transform.SetScale(origTextScaleLocal.x * scaleRatio, Axis.X);

                // pos
                // uniform spacing regardless of previous element scaling
                cumulativePos.x += (display.BaseCellWidth * colDims[c].x + display.ColSpacing) / 2.0f;
                currHeader.transform.localPosition = cumulativePos;
                cumulativePos.x += (display.BaseCellWidth * colDims[c].x + display.ColSpacing) / 2.0f;
            }

            // position and scale cells
            int numRows = display.Cells.Length / display.NumCols;
            cumulativePos = Vector3.zero;
            for (int r = 0; r < numRows; r++) {
                cumulativePos.x = -display.BaseCellWidth;
                for (int c = 0; c < display.NumCols; c++) {
                    var currCell = display.Cells[r * display.NumCols + c];
                    currCell.transform.SetParent(display.CellAnchorPos);

                    // scale
                    if (currCell.DataSlot.Displays.Length != 0)
                    {
                        if (currCell.DataSlot.Displays[0].OutputTransform != null)
                        {
                            var currScale = currCell.transform.lossyScale;
                            var origTextScaleLocal = currCell.DataSlot.Displays[0].OutputTransform.transform.localScale;
                            var origTextScaleLossy = currCell.DataSlot.Displays[0].OutputTransform.transform.lossyScale;
                            currCell.transform.SetScale(currScale * colDims[c], Axis.X);
                            var scaleRatio = new Vector3(
                                origTextScaleLossy.x / currCell.DataSlot.Displays[0].OutputTransform.transform.lossyScale.x,
                                origTextScaleLossy.y / currCell.DataSlot.Displays[0].OutputTransform.transform.lossyScale.y,
                                origTextScaleLossy.z / currCell.DataSlot.Displays[0].OutputTransform.transform.lossyScale.z
                                );
                            for (int i = 0; i < currCell.DataSlot.Displays.Length; i++)
                            {
                                currCell.DataSlot.Displays[i].OutputTransform.transform.SetScale(origTextScaleLocal.x * scaleRatio, Axis.X);
                            }
                        }
                        else
                        {
                            var currScale = currCell.transform.lossyScale;
                            currCell.transform.SetScale(currScale * colDims[c], Axis.X);
                        }
                    }

                    //pos
                    // uniform spacing regardless of previous element scaling
                    cumulativePos.x += (display.BaseCellWidth * colDims[c].x + display.ColSpacing) / 2.0f;
                    currCell.transform.localPosition = cumulativePos;
                    cumulativePos.x += (display.BaseCellWidth * colDims[c].x + display.ColSpacing) / 2.0f;
                }
                cumulativePos.y += display.RowSpacing;
            }
        }

        public static void ClearCells(PuzzleDisplay display) {
            foreach (PuzzleCell cell in display.Cells) {
                if (cell.DataSlot.Modifiable) {
                    DataUtility.ClearData(cell.DataSlot);
                }
            }
        }

        private static Vector2[] GenerateColDims(List<DataTypeMask> types)
        {
            Vector2[] newDims = new Vector2[types.Count];
            for (int i = 0; i < types.Count; i++) {
                newDims[i] = GenerateColDimsForType(types[i]);
            }

            return newDims;
        }

        private static Vector2 GenerateColDimsForType(DataTypeMask type)
        {
            var defaultDims = new Vector2(1, 1);
            if ((type & DataTypeMask.Name) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Coordinates) != 0) {
                return defaultDims * 1.2f;
            }
            if ((type & DataTypeMask.Color) != 0) {
                return defaultDims * 0.6f;
            }
            if ((type & DataTypeMask.ApparentMagnitude) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.AbsoluteMagnitude) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.MaterialSpectrum) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Temperature) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Distance) != 0) {
                return defaultDims * 0.8f;
            }
            if ((type & DataTypeMask.Historical_Coordinates) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Historical_Temperature) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Historical_Distance) != 0) {
                return defaultDims;
            }
            if ((type & DataTypeMask.Historical_Color) != 0) {
                return defaultDims;
            }

            return defaultDims;
        }
    }
}
