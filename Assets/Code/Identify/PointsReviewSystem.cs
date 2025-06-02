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
using System;

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

        private void CheckPuzzle() {
            PuzzleState puzzle = Find.State<PuzzleState>();
            if (PuzzleUtility.CheckSolutionCorrect(puzzle, out BitSet32 rowsCorrectness)) {
                ReviewUtility.OnCorrectPuzzleSubmission.Invoke(puzzle.ActivePuzzle.DisplayName);
                puzzle.PuzzleCorrectSubmissionRoutine.Replace(ReviewUtility.PuzzleCorrectSubmissionRoutine(m_State.ReviewModule, m_State, 2));

                puzzle.ActivePuzzle = null;

                Log.Msg("[PointsReviewSystem] Puzzle CORRECT! :D");
            } else {
                using (var table = TempVarTable.Alloc()) {
                    for (int r = 0; r < puzzle.ActivePuzzle.Rows.Length; r++) {
                        StringHash32 key = "row_" + r;
                        table.Set(key, rowsCorrectness[r]);
                    }
                    ScriptUtility.Trigger(ScriptEvents.IncorrectPuzzleSubmission, table);
                }
                ReviewUtility.ShowResultSprite(false, m_State);
                Log.Msg("[SubmitPuzzleSystem] Puzzle INCORRECT! D:");
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

        [DebugMenuFactory]
        private static DMInfo SpawnPuzzle() {
            DMInfo info = new DMInfo("Puzzle");
            info.AddButton("Spawn Puzzle", () => {
                Game.Events.Dispatch(GameEvents.StartPuzzleMode);

                GameLoop.ResumeUpdates(AstroGame.MonitorControlsUpdateMask);
                GameLoop.ResumeUpdates(AstroGame.SubmissionUpdateMask);
                GameLoop.ResumeUpdates(AstroGame.InstrumentUpdateMask);
            });
            return info;
        }

        private void CheckObjectIdentification() {
            ReviewResult result = ReviewUtility.EvaluateSubmission(m_State.Identification, Find.State<PlayerProgressState>());
            ReviewResult[] acceptedResults = new ReviewResult[] { ReviewResult.Success, ReviewResult.SuccessNotInNeutrinoEvent, ReviewResult.ClassificationNotInAccepted };
            ReviewUtility.ShowResultSprite(Array.IndexOf(acceptedResults, result) != -1, m_State);

            switch (result) {
                case ReviewResult.Success: {
                    ReviewUtility.AddPoints(1);
                    Game.Events.Dispatch(GameEvents.ValidOpenIdSubmission);
                    break;
                }
                case ReviewResult.SuccessNotInNeutrinoEvent: {
                    Game.Events.Dispatch(GameEvents.ValidKnowledgeSubmission);
                    break;
                }
                case ReviewResult.Duplicate: {
                    Game.Events.Dispatch(GameEvents.DuplicateOpenIdSubmission);
                    break;
                }
                case ReviewResult.ClassificationNotFound: {
                    Game.Events.Dispatch(GameEvents.IncorrectOpenIdSubmission);
                    break;
                }
                case ReviewResult.AssetNotInNeutrinoEvent: {
                    Game.Events.Dispatch(GameEvents.InvalidOpenIdSubmission);
                    break;
                }
                case ReviewResult.ClassificationNotInAccepted: {
                    Game.Events.Dispatch(GameEvents.UnacceptedOpenIdSubmission);
                    break;
                }
            }
        }
    }
}