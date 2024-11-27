using FieldDay.Systems;
using FieldDay;
using System;
using BeauUtil.Debugger;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 501)] // After RowSelectSystem

    // TODO: connect/merge with PointsReviewSystem
    public class SubmitPuzzleSystem : ComponentSystemBehaviour<SubmitButton, LabInteractable>
    {
        public override void ProcessWorkForComponent(SubmitButton primary, LabInteractable secondary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }
            PuzzleState puzzle = Find.State<PuzzleState>();
            if (PuzzleUtility.CheckSolutionCorrect(puzzle)) {
                Log.Msg("[SubmitPuzzleSystem] Puzzle CORRECT! :D");
                DocumentUtility.SpawnDocument("CorrectDocument");
            } else {
                Log.Msg("[SubmitPuzzleSystem] Puzzle INCORRECT! D:");
                // reload puzzle
                PuzzleUtility.ClearCells(puzzle.Display);
            }
            primary.gameObject.SetActive(false);
        }
    }

    public static partial class PuzzleUtility {
        public static bool CheckSolutionCorrect(PuzzleState state) {
            // convert combined flags to array of single flags
            DataTypeMask[] types = Array.FindAll((DataTypeMask[])Enum.GetValues(typeof(DataTypeMask)), t => state.ActivePuzzle.RequiredProperties.HasFlag(t));
            DataPacket refData;
            PuzzleCell currentCell;
            // iterate through rows: each row is a CelestialAsset
            for (int r = 0; r < state.ActivePuzzle.Rows.Length; r++) {
                CelestialAsset asset = Find.NamedAsset<CelestialAsset>(state.ActivePuzzle.Rows[r].Object);
                // iterate through columns: each column is a required property
                for (int c = 0; c < types.Length; c++) {
                    if (types[c] == 0x0) continue; // skip "None"
                    refData = CelestialAsset.MaskAssetToData(types[c], asset);
                    currentCell = state.Display.Cells[r * state.Display.NumCols + c];
                    if (!currentCell.DataSlot.CurrentData.Equals(refData)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
