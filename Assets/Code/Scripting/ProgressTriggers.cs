using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using UnityEngine; // Temporary
using Leaf.Runtime;

namespace Astro {
    static public class ProgressTriggers {
        static public readonly StringHash32 PointsUpdated = new StringHash32("PointsUpdated");
        static public readonly StringHash32 CorrectPuzzleSubmission = new StringHash32("CorrectPuzzleSubmission");

        [InvokeOnBoot]
        static public void Init() {
            PointsUtility.OnPointsUpdated.Register(OnScore);

            PointsReviewSystem.OnCorrectPuzzleSubmission.Register(OnCorrectPuzzleSubmit);
        }

        static private void OnScore() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("SciencePoints", PointsUtility.GetPoints());
                ScriptUtility.Trigger(PointsUpdated, table);
            }
        }

        static private void OnCorrectPuzzleSubmit() {
            using(var table = TempVarTable.Alloc()) {
                table.Set("PuzzleName", Find.State<PuzzleState>().ActivePuzzle.DisplayName);
                ScriptUtility.Trigger(CorrectPuzzleSubmission, table);
            }
        }

        [LeafMember("StartPuzzleMode")]
        static private void StartPuzzleMode() => Game.Events.Dispatch(GameEvents.PuzzleModeStart);

        [LeafMember("StartOpenMode")]
        static private void StartOpenMode() => Game.Events.Dispatch(GameEvents.OpenModeStart);
        
    }
}