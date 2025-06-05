using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.PuzzleSubmissionUpdateMask)]
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
                            DataUtility.AssignSelectedTarget(dataState, cell.DataSlot);
                        }
                    }

                    currIndex++;
                }
                puzzleState.CellsUpdated = false;
            }
        }
    }
}
