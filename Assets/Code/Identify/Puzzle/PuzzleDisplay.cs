using BeauUtil;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro
{
    public class PuzzleDisplay : BatchedComponent
    {
        public PuzzleCell[] Cells;
        public TMP_Text[] Headers;
        public int NumCols;
        public Transform CellAnchorPos;
        public Transform HeaderAnchorPos;
        public float RowSpacing;
        public float ColSpacing;
        public LabInteractable SubmitButton;
    }

    public static partial class PuzzleUtility
    {
        public static void LoadCells(PuzzleDisplay display, RingBuffer<PuzzleCell> cells, TMP_Text[] headers, int numCols)
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
        public static void LayoutCells(PuzzleDisplay display)
        {
            // position headers
            for (int c = 0; c < display.NumCols; c++)
            {
                var currHeader = display.Headers[c];
                currHeader.transform.SetParent(display.HeaderAnchorPos);
                var newPos = Vector3.zero;
                newPos.x += display.ColSpacing * c;
                currHeader.transform.localPosition = newPos;
            }

            // position cells
            int numRows = display.Cells.Length / display.NumCols;
            for (int r = 0; r < numRows; r++) {
                for (int c = 0; c < display.NumCols; c++) {
                    var currCell = display.Cells[r * display.NumCols + c];
                    currCell.transform.SetParent(display.CellAnchorPos);
                    var newPos = Vector3.zero;
                    newPos.y += display.RowSpacing * r;
                    newPos.x += display.ColSpacing * c;
                    currCell.transform.localPosition = newPos;
                }
            }
        }

        public static void ClearCells(PuzzleDisplay display) {
            foreach (PuzzleCell cell in display.Cells) {
                DataUtility.ClearData(cell.DataSlot);
            }
        }
    }
}
