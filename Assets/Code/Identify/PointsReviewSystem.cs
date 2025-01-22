using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    public class PointsReviewSystem : SharedStateSystemBehaviour<PlayerPointsState> {

        public override bool HasWork() {
            return base.HasWork() && (m_State.SubmittedObject || m_State.SubmittedPuzzle);
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
                ResetReview(m_State.ReviewModule);
            }
            
        }

        private void CheckObjectOrPuzzle() {
            if (m_State.SubmittedObject) {
                CheckObjectIdentification();
            } else if (m_State.SubmittedPuzzle) {
                // if puzzle checking is expensive, could this be amortized over the timer duration?
                CheckPuzzle();
            }
        }

        private void TryProgressPips(float timerProgress, ReviewModule module) {
            float numPips = module.CountdownSprites.Length;
            if (timerProgress * (numPips + 1) > (module.PipsRevealed + 1)) {
                module.CountdownSprites[module.PipsRevealed].enabled = true;
                module.PipsRevealed++;
            } else return;
        }

        private void ResetReview(ReviewModule module) {
            module.PipsRevealed = 0;
            foreach (SpriteRenderer pip in module.CountdownSprites) { 
                pip.enabled = false;
            }
            module.ResultSprite.enabled = false;
            m_State.SubmittedObject = m_State.SubmittedPuzzle = false;
            m_State.ReviewTimer.Paused = false;
        }

        private void ShowResultSprite(bool correct, PlayerPointsState state) {
            if (correct) {
                state.ReviewModule.ResultSprite.sprite = state.PipCorrect;
            } else {
                state.ReviewModule.ResultSprite.sprite = state.PipIncorrect;
            }
            state.ReviewModule.ResultSprite.enabled = true;
        }

        private void CheckPuzzle() {
            PuzzleState puzzle = Find.State<PuzzleState>();
            if (PuzzleUtility.CheckSolutionCorrect(puzzle)) {
                OnCorrectPuzzleSubmission.Invoke(puzzle.ActivePuzzle.DisplayName);
                ShowResultSprite(true, m_State);

                PointsUtility.AddPoints(1, m_State);

                Log.Msg("[PointsReviewSystem] Puzzle CORRECT! :D");
                DocumentUtility.SpawnDocument("CorrectDocument");
            } else {
                ShowResultSprite(false, m_State);
                Log.Msg("[SubmitPuzzleSystem] Puzzle INCORRECT! D:");
                // TODO: show incorrect cells
                PuzzleUtility.ClearCells(puzzle.Display);
            }
        }

        private void CheckObjectIdentification() {
            if (ReferenceUtility.CurrentRefMatchesFocus()) {
                ShowResultSprite(true, m_State);
                PointsUtility.AddPoints(1, m_State);
            } else {
                ShowResultSprite(false, m_State);

            }
        }

        static public readonly CastableEvent<string> OnCorrectPuzzleSubmission = new CastableEvent<string>();
    }
}