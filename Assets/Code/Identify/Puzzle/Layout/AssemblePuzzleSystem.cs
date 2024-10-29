using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    public class AssemblePuzzleSystem : ComponentSystemBehaviour<PuzzleDisplay>
    {
        public override void ProcessWork(float deltaTime)
        {
            // Only assemble the puzzle if new one is queued or TODO reset clicked
            var state = Find.State<PuzzleState>();
            if (state.QueuedPuzzle == null) { return; }

            foreach (var display in m_Components)
            {

            }

            int numCols = PuzzleUtility.NumCols(state.QueuedPuzzle);
            state.SelectedCells = new bool[state.QueuedPuzzle.Rows.Length, numCols];
            // select first row by default
            for (int i = 0; i < numCols; i++) {
                state.SelectedCells[0, i] = true;
            }
            state.CellsUpdated = true;

            Debug.Log("[AssemblePuzzle] New puzzle cols: " + numCols);

            state.ActivePuzzle = state.QueuedPuzzle;
            state.QueuedPuzzle = null;
        }
    }
}