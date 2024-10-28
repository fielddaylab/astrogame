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
            if (puzzleState.RowUpdated) {
                int currIndex = 0;
                foreach(var component in m_Components)
                {
                    PuzzleUtility.UpdateRowVisuals(component, currIndex == puzzleState.SelectedRow);

                    if (currIndex == puzzleState.SelectedRow)
                    {
                        var dataState = Find.State<DataTransferState>();
                        dataState.SelectedTarget = component.DataSlot;
                    }

                    currIndex++;
                }

                puzzleState.RowUpdated = false;
            }
        }
    }
}
