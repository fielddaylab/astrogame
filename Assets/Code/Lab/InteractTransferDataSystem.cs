using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000, AstroGame.InstrumentUpdateMask)] // After RowSelectSystem
    public class InteractTransferDataSystem : ComponentSystemBehaviour<InteractTransferData, LabInteractable>
    {
        public override void ProcessWorkForComponent(InteractTransferData primary, LabInteractable secondary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }

            // Transfer data on interact if there is a valid destination
            var dataState = Find.State<DataTransferState>();
            if (dataState.SelectedTarget != null && dataState.SelectedSource != null && dataState.SelectedSource.HasData && dataState.SelectedTarget.Modifiable) {
                if (DataUtility.TryTransferData(dataState.SelectedSource, dataState.SelectedTarget)) {
                    Debug.Log("[InteractTransferSystem] Transfer success");
                    PuzzleUtility.CheckEnableSubmit(Find.State<PuzzleState>());
                    DataUtility.ClearSelections(dataState);
                    return;
                }
            }
            Debug.Log("[InteractTransferSystem] Transfer unsuccessful");
            // bandaid fix for weird behavior - locked instruments' load buttons stuck in "interact received" and spamming "transfer unsuccessful"
            secondary.InteractReceived = false;
        }
    }

}