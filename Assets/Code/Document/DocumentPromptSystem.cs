using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.Debugging;
using BeauPools;
using UnityEditor;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 100, AstroGame.DocumentUpdateMask)] // After MouseInteractionSystem

    public class DocumentPromptSystem : ComponentSystemBehaviour<DocumentInteractable, DocumentPrompter> { 
        DocumentInteractable doc;
        DocumentPrompter prompter;

        public override void ProcessWork(float deltaTime) {
            var puzzleState = Find.State<DocumentPuzzleState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var component in m_Components) {
                doc = component.Primary;
                prompter = component.Secondary;
                // determine whether this document is being dragged
                if (!component.Primary.IsDragging) {
                    continue;
                }

                // overlap box to find which document this is overlapping
                DocumentUtility.OverlapBoxAtPos(boardState, component.Primary.Renderer, out DocumentRenderer hit);

                // record last known hover asset
                DocumentUtility.UpdatePuzzleHoverAsset(puzzleState, hit);
                if (Game.IsDevBuild) {
                    if (DebugInput.IsPressed(KeyCode.P)) {
                        DebugFlags.ToggleFlag(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo);
                    }

                    DrawDebugDocumentDisplay(component.Primary, puzzleState);
                }
            }

            if (boardState.DraggablePlacedThisFrame && puzzleState.CurrHoverDoc != null && boardState.DraggablePlaced.GetComponent<DocumentPrompter>()) {
                // hovering ended; placement script trigger
                using (var table = TempVarTable.Alloc()) {
                    table.Set("documentId", puzzleState.CurrHoverDoc.Interactable.AssetName);
                    ScriptUtility.Trigger(ScriptEvents.DocumentPuzzlePromptStart, table);
                }

                // move question to specific position relative to document
                boardState.DocumentRoutine.Replace(DocumentUtility.MoveAboveRelativeToDoc(boardState.DraggablePlaced, puzzleState.CurrHoverDoc));
                // clear document highlight
                DocumentUtility.SetDocumentHighlight(puzzleState.CurrHoverDoc, Color.white);
            }

        }

        void OnDrawGizmosSelected() {
            if (doc == null || prompter == null) return;
            var boardState = Find.State<DocumentBoardState>();

            Vector3 docExtents = new Vector3(doc.Renderer.Size.width / 2, doc.Renderer.Size.height / 2, 1);
            var position = doc.transform.position + new Vector3(0f, doc.Renderer.Size.y, 0f);

            var oldMatrix = Gizmos.matrix;
            Gizmos.color = Color.green;
            Gizmos.matrix = Matrix4x4.TRS(position, doc.transform.rotation, docExtents * 2);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldMatrix;
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
 
        }
    }
}