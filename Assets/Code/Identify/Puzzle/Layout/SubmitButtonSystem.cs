using FieldDay.Systems;
using FieldDay;
using System;
using BeauUtil.Debugger;
using System.Collections;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 501, AstroGame.SubmissionUpdateMask)] // After RowSelectSystem

    // TODO: connect/merge with PointsReviewSystem
    public class SubmitButtonSystem : ComponentSystemBehaviour<SubmitButton, LabInteractable> {
        public override void ProcessWorkForComponent(SubmitButton primary, LabInteractable secondary, float deltaTime) {          
            if (!secondary.InteractReceived) { return; }
            if ((primary.ButtonType & SubmitButtonType.SubmitPuzzle) != 0) {
                if (Find.State<PuzzleState>().ActivePuzzle != null) {
                    TrySubmitPuzzle(primary);
                }
            }
            if ((primary.ButtonType & SubmitButtonType.SubmitIdentification) != 0) {
                TrySubmitIdentification(primary);
            }
        }

        private bool TrySubmitPuzzle(SubmitButton btn) {
            PlayerPointsState pps = Find.State<PlayerPointsState>();
            if (!pps.SubmittedPuzzle) {
                pps.SubmittedPuzzle = true;
                btn.gameObject.SetActive(false);
                return true;
            }
            return false;
        }

        private bool TrySubmitIdentification(SubmitButton btn) {
            PlayerPointsState pps = Find.State<PlayerPointsState>();
            if (!pps.SubmittedObject) {
                pps.SubmittedObject = true;
                btn.gameObject.SetActive(false);
                return true;
            }
            return false;
        }
    }

    public static partial class PuzzleUtility {

        public static void SetButtonMode(SubmitButton button, SubmitButtonType type) {
            button.ButtonType = type;
        }
        public static bool CheckSolutionCorrect(PuzzleState state, out BitArray rowsCorrect) {
            // convert combined flags to array of single flags
            DataTypeMask[] types = Array.FindAll((DataTypeMask[])Enum.GetValues(typeof(DataTypeMask)), t => state.ActivePuzzle.RequiredProperties.HasFlag(t));
            DataPacket refData;
            PuzzleCell currentCell;
            bool allCorrect = true;
            // iterate through rows: each row is a CelestialAsset
            rowsCorrect = new BitArray(state.ActivePuzzle.Rows.Length);
            for (int r = 0; r < state.ActivePuzzle.Rows.Length; r++) {
                rowsCorrect[r] = true;
                CelestialAsset asset = Find.NamedAsset<CelestialAsset>(state.ActivePuzzle.Rows[r].Object);
                // iterate through columns: each column is a required property
                for (int c = 0; c < types.Length; c++) {
                    if (types[c] == 0x0) continue; // skip "None"
                    refData = CelestialAsset.MaskAssetToData(types[c], asset);
                    currentCell = state.Display.Cells[r * state.Display.NumCols + c];
                    if (!currentCell.DataSlot.CurrentData.Equals(refData)) {
                        rowsCorrect[r] = false;
                        allCorrect = false;
                        break;
                    }
                }
            }
            return allCorrect;
        }
    }
}
