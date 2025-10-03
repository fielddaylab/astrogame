using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Debugging;
using BeauPools;
using System.Collections.Generic;

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

                    bool isCoordinateCell = dataState.SelectedTarget.PuzzleCol == 1;

                    if (isCoordinateCell) {
                        int rowIndex = dataState.SelectedTarget.PuzzleRow;
                        UIFocus current = Find.State<FocusState>().CurrentFocus;

                        // Ensure none of the other rows have this data
                        for (int i = 0; i < puzzleState.PuzzleEntryGuesses.Length; i++) {
                            if (puzzleState.PuzzleEntryGuesses[i] == null) continue;

                            PuzzleUtility.ExtractCols(puzzleState.ActivePuzzle, out List<DataTypeMask> types, out int numCols);
                            if (current == puzzleState.PuzzleEntryGuesses[i]) {
                                int cellIndex = (numCols * i) + 1;
                                PuzzleCell target = puzzleState.Display.Cells[cellIndex];
                                // Found a match we need to clear now
                                DataUtility.ClearData(target.DataSlot);
                                FocusableUtility.UpdateFocusTrackerSprite(puzzleState.PuzzleEntryGuesses[i], null);
                                puzzleState.PuzzleEntryGuesses[i] = null;
                            }
                        }

                        // clear any previous stars we put this guess tracker on
                        if (puzzleState.PuzzleEntryGuesses[rowIndex] != null) {
                            FocusableUtility.UpdateFocusTrackerSprite(puzzleState.PuzzleEntryGuesses[rowIndex], null);
                        }
                        FocusableUtility.UpdateFocusTrackerSprite(current, FocusState.GuessTrackerSprites[rowIndex]);
                        puzzleState.PuzzleEntryGuesses[rowIndex] = current;     
                    }

                    if (DebugFlags.IsFlagSet(FocusState.DebuggingFlags.DisplayGuessTrackerInfo)) {
                        using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                            psb.Builder.Append("Current Tracker Set: [");
                            foreach (var entry in puzzleState.PuzzleEntryGuesses) {
                                if (entry == null) {
                                    psb.Builder.Append("X,");
                                    continue;
                                }
                                psb.Builder.Append(entry.TargetData.DisplayName);
                                psb.Builder.Append(",");
                            }
                            psb.Builder.Append(']');

                            DebugDraw.AddLogText(psb, Color.yellow, 1000f);
                        }
                    }

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
                } else {
                    Debug.Log("[InteractTransferSystem] Transfer unsuccessful");
                }
            }

        }
    }

}