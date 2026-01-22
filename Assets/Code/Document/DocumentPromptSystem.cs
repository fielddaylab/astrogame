using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Debugging;
using BeauPools;
using static UnityEngine.Rendering.DebugUI;
using BeauUtil;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 100, AstroGame.DocumentUpdateMask)] // After MouseInteractionSystem

    public class DocumentPromptSystem : ComponentSystemBehaviour<DocumentInteractable, DocumentPrompter> { 
        public override void ProcessWork(float deltaTime) {
            var puzzleState = Find.State<DocumentPuzzleState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var component in m_Components) {
                // determine whether this document is being dragged
                if (!component.Primary.IsDragging) {
                    continue;
                }

                var renderer = component.Primary.Renderer;

                // overlap box to find which document this is overlapping
                DocumentUtility.OverlapBoxAtPos(boardState, renderer, out DocumentRenderer hit);
                if (Game.IsDevBuild && DebugFlags.IsFlagSet(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo)) {
                    DrawDebugDocumentDisplay(component.Primary, puzzleState);
                }

                // record last known hover asset
                DocumentUtility.UpdatePuzzleHoverAsset(puzzleState, hit);
            }

            // set debugging flags
            if (Game.IsDevBuild){
                if (DebugInput.IsPressed(KeyCode.LeftBracket)) {
                    DebugFlags.ToggleFlag(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo);
                }
            }

            // disallow placement on other question documents
            if (boardState.DraggablePlacedThisFrame && puzzleState.CurrHoverDoc != null && puzzleState.CurrHoverDoc.TriggersPrompter && boardState.DraggablePlaced.GetComponent<DocumentPrompter>()) {
                // hovering ended; placement script trigger
                using (var table = TempVarTable.Alloc()) {
                    table.Set("questionId", boardState.DraggablePlaced.AssetName);
                    table.Set("answerId", puzzleState.CurrHoverDoc.Interactable.AssetName);
                    ScriptUtility.Trigger(ScriptEvents.DocumentPuzzlePromptStart, table);
                }

                DocumentUtility.UpdateDocPuzzleAnswer(puzzleState, boardState.DraggablePlaced.AssetName, puzzleState.CurrHoverDoc.Interactable.AssetName);

                // clear document highlight
                DocumentUtility.SetDocumentHighlight(puzzleState.CurrHoverDoc, Color.white);

                // Check if all puzzle questions are correct
                if (DocumentUtility.IsDocPuzzleCorrect(puzzleState)) {
                    ScriptUtility.Trigger(ScriptEvents.DocumentPuzzleSolved);
                    AstroGame.Events.Dispatch(GameEvents.PostitMatchAccepted);
                }
                else {
                    AstroGame.Events.Dispatch(GameEvents.PostitMatchRejected);
                }
            }
            else if (boardState.DraggablePlacedThisFrame) {
                // placed onto nothing
                DocumentUtility.UpdateDocPuzzleAnswer(puzzleState, boardState.DraggablePlaced.AssetName, StringHash32.Null);
            }

        }

        private void DrawDebugDocumentDisplay(DocumentInteractable doc, DocumentPuzzleState state){
            if (!DebugFlags.IsFlagSet(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo)) return;

            using (PooledStringBuilder psb = PooledStringBuilder.Create()) {
                psb.Builder.Append("Question Document: ");
                psb.Builder.Append(doc.name);

                DebugDraw.AddLogText(psb, Color.white);
                psb.Builder.Clear();

                psb.Builder.Append("Hovering over: ");
                if (state.CurrHoverDoc) {
                    psb.Builder.Append(state.CurrHoverDoc.name);
                } else {
                    psb.Builder.Append("None");
                }
                DebugDraw.AddLogText(psb, Color.yellow);
            }
                
            var renderer = doc.Renderer;
            Vector3 docExtents = new Vector3(renderer.Size.width / 2, renderer.Size.height / 2, 1);
            Vector3 position = renderer.transform.position + new Vector3(0f, renderer.Size.y, 0f);
            Matrix4x4 m = Matrix4x4.TRS(position, renderer.transform.rotation, docExtents * 2);
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(1, 1, 0));
            DebugDraw.AddOrientedBounds(m, bounds, Color.green, 0.1f); 
        }
    }
}