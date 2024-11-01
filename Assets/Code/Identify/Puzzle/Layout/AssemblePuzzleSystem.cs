using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;

namespace Astro
{
    public class AssemblePuzzleSystem : ComponentSystemBehaviour<PuzzleDisplay>
    {
        private readonly RingBuffer<PuzzleCell> m_CellWorkList = new RingBuffer<PuzzleCell>(12);

        public override void ProcessWork(float deltaTime)
        {
            // Only assemble the puzzle if new one is queued or TODO reset clicked
            var state = Find.State<PuzzleState>();
            if (state.QueuedPuzzle == null) { return; }

            var pools = Find.State<PuzzlePools>();
            PuzzleUtility.ExtractCols(state.QueuedPuzzle, out List<DataTypeMask> types, out int numCols);

            m_CellWorkList.Clear();

            foreach (var display in m_Components)
            {
                // TODO: generate header labels

                // generate and organize puzzle cells
                for (int r = 0; r < state.QueuedPuzzle.Rows.Length; r++) {
                    for (int c = 0; c < numCols; c++) {
                        var newCell = pools.Cells.Alloc(display.transform.position);
                        newCell.DataSlot.Type = types[c];
                        m_CellWorkList.PushBack(newCell);
                    }
                }

                    PuzzleUtility.LoadCells(display, m_CellWorkList, numCols);
                PuzzleUtility.LayoutCells(display);
            }

            state.SelectedCells = new bool[state.QueuedPuzzle.Rows.Length, numCols];
            // select first row by default
            for (int i = 0; i < numCols; i++) {
                state.SelectedCells[0, i] = true;
            }
            state.CellsUpdated = true;

            Debug.Log("[AssemblePuzzle] New puzzle cols: " + numCols);

            state.ActivePuzzle = state.QueuedPuzzle;
            state.QueuedPuzzle = null;
            m_CellWorkList.Clear();
        }
    }
}