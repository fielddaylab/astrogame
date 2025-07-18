
using FieldDay;
using FieldDay.Systems;

using BeauUtil;
using BeauRoutine;
using BeauUtil.Debugger;
using Astro.Reference;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 501, AstroGame.AnySubmissionUpdateMask)] // After RowSelectSystem

    // TODO: connect/merge with ReviewSystem
    public class SubmitButtonSystem : ComponentSystemBehaviour<SubmitButton, LabInteractable> {
        public override void ProcessWorkForComponent(SubmitButton primary, LabInteractable secondary, float deltaTime) {          
            if (!secondary.InteractReceived) { return; }

            switch (primary.ButtonType) {
                case SubmitButtonType.SubmitIdentification:
                    TrySubmitIdentification(primary);
                    break;
                
                case SubmitButtonType.SubmitPuzzle:
                    if (Find.State<PuzzleState>().ActivePuzzle == null) break;

                    TrySubmitPuzzle(primary);
                    break;
                
                case SubmitButtonType.Decoder:
                    Log.Msg("[SubmitButtonSystem] assessing puzzle state");
                    TrySubmitDecoderPasscode(primary);
                    break;
                
                default:
                    Log.Warn("[SubmitButtonSystem] submit button state unrecognized.");
                    break;
            }
        }

        private bool TrySubmitDecoderPasscode(SubmitButton btn) {
            ReviewState reviewState = Find.State<ReviewState>();
            if (reviewState.CurrentSubmission == ReviewSubmissionType.None) {
                reviewState.CurrentSubmission = ReviewSubmissionType.Decoder;
                Routine.Start(btn.SetButtonActive(false));
                return true;
            }
            return false;
        }

        private bool TrySubmitPuzzle(SubmitButton btn) {
            ReviewState reviewState = Find.State<ReviewState>();
            if (reviewState.CurrentSubmission == ReviewSubmissionType.None) {
                reviewState.CurrentSubmission = ReviewSubmissionType.Puzzle;
                btn.PuzzleLoadingCollider.SetActive(true);
                Routine.Start(btn.SetButtonActive(false));
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
            btn.MonitorLoadingCollider.SetActive(true);
            Routine.Start( btn.SetButtonActive(false) );
            ConsoleTypedText console = Find.FirstComponent<ConsoleTypedText>();
            
            if (rgs.SelectedRefClassification != null) {
                console.PlayClassification(rgs.SelectedRefClassification);
            } else if (rgs.SelectedMaterials != 0){
                console.PlayMaterials(rgs.SelectedMaterials);
            }
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
