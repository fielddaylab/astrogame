using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    public class RowSelectSystem : ComponentSystemBehaviour<InteractRowSelector, LabInteractable>
    {
        public override void ProcessWorkForComponent(InteractRowSelector primary, LabInteractable secondary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }

            var puzzleState = Find.State<PuzzleState>();
            var dataState = Find.State<DataTransferState>();
            PuzzleUtility.TrySetSelectedRow(puzzleState, puzzleState.SelectedRow + primary.SelectDir);

        }
    }
}
