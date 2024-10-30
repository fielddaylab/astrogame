using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000)] // After RowSelectSystem
    public class CellDisplayUpdateSystem : ComponentSystemBehaviour<PuzzleDisplay>
    {
        public override void ProcessWork(float deltaTime)
        {
            var puzzleState = Find.State<PuzzleState>();
            if (puzzleState.GroupCellsByRow) { return; }

            if (puzzleState.CellsUpdated)
            {
                foreach (var component in m_Components)
                {
                    for (int r = 0; r < puzzleState.SelectedCells.GetLength(1); r++)
                    {
                        for (int c = 0; c < puzzleState.SelectedCells.GetLength(0); c++)
                        {
                            bool visible = puzzleState.SelectedCells[r, c] && ((component.Rows[r].Cells[c].DataSlot.Type & puzzleState.RelevantColFilter) != 0);
                            PuzzleUtility.UpdateCellVisuals(puzzleState, component.Rows[r].Cells[c], visible);
                        }
                    }
                }

                puzzleState.CellsUpdated = false;
            }
        }
    }
}
