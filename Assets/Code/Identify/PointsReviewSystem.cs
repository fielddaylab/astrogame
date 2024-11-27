using FieldDay;
using FieldDay.Systems;

namespace Astro {
    public class PointsReviewSystem : SharedStateSystemBehaviour<PlayerPointsState> {

        public override bool HasWork() {
            return m_State.SubmittedObject || m_State.SubmittedPuzzle;
        }
        public override void ProcessWork(float deltaTime) {
            // TODO: implement
            if (!m_State.ReviewTimer.Advance(deltaTime)) {
                // If (review timer passes an increment of 1/(pips+1))
                    // reveal a new pip
            } else {
                if (m_State.SubmittedObject) {
                    CheckObjectIdentification();
                } else if (m_State.SubmittedPuzzle) {
                    // if puzzle checking is expensive, could this be amortized over the timer duration?
                    CheckPuzzle();
                }
            }      
        }

        private void RevealPip(float timerProgress, float numPips, int activePips) {
            if (timerProgress * (numPips+1) > (activePips+1)) {

            }
        }

        private void CheckPuzzle() {
            // TODO: implement
            if (PuzzleUtility.CheckSolutionCorrect(Find.State<PuzzleState>())) {
                // show correct sprite
                // add points
            } else {
                // show incorrect sprite
                // decrement points?
            }
        }

        private void CheckObjectIdentification() {
            // TODO: implement

            // if (object identification correct)
                // show correct sprite
                // add points
            // else
                // show incorrect sprite
                // decrement points?
        }
    }
}