using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Rendering;
using FieldDay.Scripting;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;
using Astro.Reference;
using FieldDay.Audio;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.SubmissionUpdateMask)]
    public class PointsReviewSystem : SharedStateSystemBehaviour<ReviewState> {

        public override bool HasWork() {
            return base.HasWork() && (m_State.CurrentSubmission != 0);
        }

        public override void ProcessWork(float deltaTime) {
            if (m_State.ReviewTimer.Advance(deltaTime)) {
                CheckObjectOrPuzzle();
                m_State.ReviewCooldown.Paused = false;
                return;
            } else if (!m_State.ReviewTimer.Paused) {
                TryProgressPips(m_State.ReviewTimer.GetProgress(), m_State.ReviewModule);
                return;
            } 
            if (m_State.ReviewCooldown.Advance(deltaTime)) {
                ReviewModuleUtility.ResetReview( m_State.ReviewModule );
            } 
        }

        private void CheckObjectOrPuzzle() {
            if (m_State.CurrentSubmission == ReviewSubmissionType.Identification) {
                CheckObjectIdentification();
            } 
            if (m_State.CurrentSubmission == ReviewSubmissionType.Puzzle) {
                // if puzzle checking is expensive, could this be amortized over the timer duration?
                CheckPuzzle();
            }
        }

        private void TryProgressPips(float timerProgress, ReviewModule module) {
            float numPips = module.CountdownSprites.Length;
            if (timerProgress * (numPips + 1) > (module.PipsRevealed + 1)) {
                module.CountdownSprites[module.PipsRevealed].SetSharedMaterialAtIndex(1, module.LitPipMaterial);
                module.PipsRevealed++;
                Sfx.PlayDetached(module.PipCountSounds[module.PipsRevealed], module.SoundAnchor);
            } else return;
        }

        static private void ShowResultSprite(bool correct, ReviewState state) {
            ReviewModule module = state.ReviewModule;

            if (correct) {
                Sfx.PlayDetached("Oneshot.Review.Success", module.SoundAnchor);
                module.Result.SetSharedMaterialAtIndex(1, module.SuccessMaterial);
            } else {
                Sfx.PlayDetached("Oneshot.Review.Failure", module.SoundAnchor);
                module.Result.SetSharedMaterialAtIndex(1, module.FailureMaterial);
            }
        }

        private void CheckPuzzle() {
            PuzzleState puzzle = Find.State<PuzzleState>();
            if (PuzzleUtility.CheckSolutionCorrect(puzzle, out BitSet32 rowsCorrectness)) {
                ReviewUtility.OnCorrectPuzzleSubmission.Invoke(puzzle.ActivePuzzle.DisplayName);
                ShowResultSprite(true, m_State);
                ReviewUtility.AddPoints(1);

                puzzle.ActivePuzzle = null;

                Log.Msg("[PointsReviewSystem] Puzzle CORRECT! :D");
            } else {
                ScriptUtility.Trigger(ScriptEvents.IncorrectPuzzleSubmission);
                ShowResultSprite(false, m_State);
                Log.Msg("[SubmitPuzzleSystem] Puzzle INCORRECT! D:");
                // TODO: show incorrect cells
                PuzzleUtility.ClearRows(puzzle, rowsCorrectness);
            }
        }

        [DebugMenuFactory]
        private static DMInfo SubmitCorrectPuzzleSolution() {
            DMInfo info = new DMInfo("Puzzle");
            info.AddButton("Bypass Puzzle", () => {
                var puzzle = Find.State<PuzzleState>();
                ReviewUtility.OnCorrectPuzzleSubmission.Invoke(puzzle.ActivePuzzle.DisplayName);
                puzzle.ActivePuzzle = null;
            });
            return info;
        }

        private void CheckObjectIdentification() {
            ReviewResult result = ReviewUtility.EvaluateSubmission(m_State.Identification, Find.State<PlayerProgressState>());
            ShowResultSprite(result == ReviewResult.Success, m_State);
            switch (result) {
                case ReviewResult.Success: {
                    ReviewUtility.AddPoints(1);
                    break;
                }
                case ReviewResult.Duplicate: {
                    ScriptUtility.Trigger(ScriptEvents.OnDuplicateOpenIdSubmission);
                    break;
                }
                case ReviewResult.ClassificationNotFound: {
                    ScriptUtility.Trigger(ScriptEvents.OnIncorrectOpenIdSubmission);
                    break;
                }
                case ReviewResult.AssetNotInNeutrinoEvent: {
                    ScriptUtility.Trigger(ScriptEvents.OnInvalidOpenIdSubmission);
                    break;
                }
                case ReviewResult.ClassificationNotInAccepted: {
                    ScriptUtility.Trigger(ScriptEvents.OnUnacceptedOpenIdSubmission);
                    break;
                }
            }
        }
    }
}