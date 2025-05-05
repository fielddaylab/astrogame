using FieldDay.Systems;
using FieldDay;
using System;
using BeauUtil.Debugger;
using System.Collections;
using BeauUtil;
using Astro.Reference;

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
            ReviewState pps = Find.State<ReviewState>();
            if (pps.CurrentSubmission == ReviewSubmissionType.None) {
                pps.CurrentSubmission = ReviewSubmissionType.Puzzle;
                btn.Root.SetActive(false);
                return true;
            }
            return false;
        }

        private bool TrySubmitIdentification(SubmitButton btn) {
            ReviewState pps = Find.State<ReviewState>();
            RefGuideState rgs = Find.State<RefGuideState>();
            if (pps.CurrentSubmission != ReviewSubmissionType.None) return false;

            pps.CurrentSubmission = ReviewSubmissionType.Identification;

            StringHash32 classId = null;
            if(rgs.SelectedRefClassification != null) {
                classId = rgs.SelectedRefClassification.AssetId;
            }
            pps.Identification = new ReviewSubmissionClassification() {
                AssetId = Find.State<FocusState>().CurrentFocus.TargetData.AssetId,
                Classification = classId,
                Materials = rgs.SelectedMaterials
            };
            btn.Root.SetActive(false);
            return true;
        }
    }

    public static partial class PuzzleUtility {

        public static void SetButtonMode(SubmitButton button, SubmitButtonType type) {
            button.ButtonType = type;
        }
        public static bool CheckSolutionCorrect(PuzzleState state, out BitSet32 rowsCorrect) {

            // convert combined flags to array of single flags
            DataTypeMask types = state.ActivePuzzle.RequiredProperties;
            
            DataPacket refData;
            PuzzleCell currentCell;
            bool allCorrect = true;
            // iterate through rows: each row is a CelestialAsset
            rowsCorrect = new BitSet32();
            
            for (int r = 0; r < state.ActivePuzzle.Rows.Length; r++) {
                rowsCorrect[r] = true;
                CelestialAsset asset = Find.NamedAsset<CelestialAsset>(state.ActivePuzzle.Rows[r].Object);
                // iterate through columns: each column is a required property
                int c = 0;
                foreach (var type in Bits.Enumerate(types)) {
                    if (type == 0) {
                        c++;
                        continue;
                    }

                    refData = CelestialAsset.MaskAssetToData(type, asset);
                    currentCell = state.Display.Cells[r * state.Display.NumCols + c];
                    if (!currentCell.DataSlot.CurrentData.Equals(refData)) {
                        rowsCorrect[r] = false;
                        allCorrect = false;
                        break;
                    }

                    c++;
                }
            }

            return allCorrect;
        }
    }
}
