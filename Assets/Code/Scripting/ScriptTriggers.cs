using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using Leaf.Runtime;

namespace Astro {
    public static class ScriptTriggers {
        [InvokeOnBoot]
        static public void Init() {
            PointsUtility.OnPointsUpdated.Register(OnScore);

            PointsReviewSystem.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);
        }

        static private void OnScore() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("sciencePoints", PointsUtility.GetPoints());
                ScriptUtility.Trigger(ScriptEvents.PointsUpdated, table);
            }
        }

        static private void OnCorrectPuzzleSubmit() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("puzzleName", Find.State<PuzzleState>().ActivePuzzle.DisplayName);
                ScriptUtility.Trigger(ScriptEvents.CorrectPuzzleSubmission, table);
            }
        }

        [LeafMember("StartPuzzleMode")]
        static private void LeafStartPuzzleMode() {
            Game.Events.Dispatch(GameEvents.PuzzleModeStart);
        }

        [LeafMember("StartOpenMode")]
        static private void LeafStartOpenMode(){
            Game.Events.Dispatch(GameEvents.OpenModeStart);
        }
        
    }
}