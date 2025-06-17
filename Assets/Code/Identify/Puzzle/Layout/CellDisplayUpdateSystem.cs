using BeauRoutine;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000, AstroGame.PuzzleSubmissionUpdateMask)] // After RowSelectSystem
    public class CellDisplayUpdateSystem : ComponentSystemBehaviour<PuzzleDisplay>
    {
        public override void ProcessWork(float deltaTime)
        {
            var puzzleState = Find.State<PuzzleState>();
            if (puzzleState.GroupCellsByRow) { return; }

            if (puzzleState.CellsUpdated) {
                foreach (var component in m_Components) {
                    for (int r = 0; r < puzzleState.SelectedCells.GetLength(0); r++) {
                        for (int c = 0; c < puzzleState.SelectedCells.GetLength(1); c++) {
                            bool visible =
                                puzzleState.SelectedCells[r, c]
                                && ((component.Cells[r * component.NumCols + c].DataSlot.Type & puzzleState.RelevantColFilter) != 0)
                                && component.Cells[r * component.NumCols + c].DataSlot.Modifiable;
                            PuzzleUtility.UpdateCellVisuals(puzzleState, component.Cells[r * component.NumCols + c], visible);
                        }
                    }
                }
                puzzleState.CellsUpdated = false;
            }
        }
    }

    public static partial class PuzzleUtility {

        public static void CheckEnableSubmit(PuzzleState state) {
            if (CheckFullyPopulated(state)) {
                ScriptUtility.Trigger(ScriptEvents.OnPuzzleGridFullyPopulated);
                Routine.Start( state.Display.SubmitButton.SetButtonActive(true) );
            } 
        }

        public static bool CheckFullyPopulated(PuzzleState state = null) {
            if (state == null) state = Find.State<PuzzleState>();

            for (int i = 0; i < state.Display.Cells.Length; i++) {
                if (!state.Display.Cells[i].DataSlot.HasData) return false;
            }
            return true;
        }
    }
}
