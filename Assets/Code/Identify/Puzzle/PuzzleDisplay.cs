using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Astro
{
    public class PuzzleDisplay : BatchedComponent
    {
        public PuzzleCell[] Cells;
        public int NumCols;
        public Transform CellAnchorPos;
        public float RowSpacing;
        public float ColSpacing;
        public LabInteractable SubmitButton;
    }

    public static partial class PuzzleUtility
    {
        public static void LoadCells(PuzzleDisplay display, RingBuffer<PuzzleCell> cells, int numCols)
        {
            display.SubmitButton.gameObject.SetActive(false);

            int numRows = cells.Count / numCols;
            display.Cells = new PuzzleCell[numRows * numCols];
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
    }
}
