using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    public class RowDisplayUpdateSystem : ComponentSystemBehaviour<PuzzleRow>
    {
        public override void ProcessWork(float deltaTime)
        {
            var puzzleState = Find.State<PuzzleState>();
            if (!puzzleState.GroupCellsByRow) { return; }

            if (puzzleState.CellsUpdated) {
                int currIndex = 0;
                foreach(var component in m_Components)
                {
                    foreach (var cell in component.Cells)
                    {
                        PuzzleUtility.UpdateCellVisuals(puzzleState, cell, currIndex == puzzleState.SelectedRow);

                        if (currIndex == puzzleState.SelectedRow)
                        {
                            var dataState = Find.State<DataTransferState>();
                            dataState.SelectedTarget = cell.DataSlot;
                        }
                    }

                    currIndex++;
                }

                puzzleState.CellsUpdated = false;
            }
        }
    }
}
