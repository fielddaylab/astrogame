using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000)] // After RowSelectSystem
    public class InteractTransferDataSystem : ComponentSystemBehaviour<LabInteractable, InteractTransferData>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, InteractTransferData secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }

            // Transfer data on interact if there is a valid destination
            var dataState = Find.State<DataTransferState>();
            if (dataState.SelectedTarget != null && dataState.SelectedSource != null && dataState.SelectedSource.HasData && dataState.SelectedTarget.Modifiable) {
                if (DataUtility.TryTransferData(dataState.SelectedSource, dataState.SelectedTarget)) {
                    Debug.Log("[InteractTransferSystem] Transfer success");
                    PuzzleUtility.CheckEnableSubmit(Find.State<PuzzleState>());
                    return;
                }
            }
            Debug.Log("[InteractTransferSystem] Transfer unsuccessful");
        }
    }

}