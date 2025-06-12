using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.Scripting;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 1000, AstroGame.InstrumentUpdateMask)] // After RowSelectSystem
    public class InteractTransferDataSystem : ComponentSystemBehaviour<InteractTransferData, LabInteractable> {
        public override void ProcessWorkForComponent(InteractTransferData primary, LabInteractable secondary, float deltaTime) {
            if (!secondary.InteractReceived) { return; }

            // Transfer data on interact if there is a valid destination
            var dataState = Find.State<DataTransferState>();
            if (dataState.SelectedTarget != null && dataState.SelectedSource != null && dataState.SelectedSource.HasData && dataState.SelectedTarget.Modifiable) {
                var targetSlotId = dataState.SelectedTarget.SlotId;
                if (DataUtility.TryTransferData(dataState.SelectedSource, dataState.SelectedTarget)) {
                    Debug.Log("[InteractTransferSystem] Transfer success");
                    var puzzleState = Find.State<PuzzleState>();
                    PuzzleUtility.CheckEnableSubmit(puzzleState);

                    bool isRowComplete = true;
                    for (int i = 0; i < puzzleState.Display.Cells.Length; i++) {
                        var currentCell = puzzleState.Display.Cells[i].DataSlot;

                        if (currentCell.PuzzleRow != dataState.SelectedTarget.PuzzleRow) continue;

                        if (!currentCell.HasData) {
                            isRowComplete = false;
                            break;
                        }
                    }

                    if (isRowComplete) {
                        FocusableUtility.UpdateFocusTrackerSprite(Find.State<FocusState>().CurrentFocus, FocusState.GuessTrackerSprites[dataState.SelectedTarget.PuzzleRow]);
                         
                        using (var table = TempVarTable.Alloc()) {
                            table.Set("rowId", dataState.SelectedTarget.PuzzleRow);
                            ScriptUtility.Trigger(ScriptEvents.OnPuzzleRowFilled, table);
                        } 
                    } else { 
                        using (var table = TempVarTable.Alloc()) {
                            table.Set("cellId", targetSlotId);
                            ScriptUtility.Trigger(ScriptEvents.OnPuzzleCellFilled, table);
                        }
                    }

                    DataUtility.ClearSelections(dataState);

                    return;
                }
                else {
                    Debug.Log("[InteractTransferSystem] Transfer unsuccessful");
                }
            }

        }
    }

}