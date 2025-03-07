using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.Scripting;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 100)] // After MouseInteractionSystem
    public class DocumentPromptSystem : ComponentSystemBehaviour<DocumentInteractable, DocumentPrompter>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            var puzzleState = Find.State<DocumentPuzzleState>();
            var boardState = Find.State<DocumentBoardState>();

            foreach (var component in m_Components)
            {
                // determine whether this document is being dragged
                if (!component.Primary.IsDragging) {
                    break;
                }

                // overlap box to find which document this is overlapping
                DocumentUtility.OverlapBoxAtPos(boardState, component.Primary.Renderer, out DocumentRenderer hit);

                // record last known hover asset
                DocumentUtility.UpdatePuzzleHoverAsset(puzzleState, hit);
            }

            if (boardState.DraggablePlacedThisFrame && puzzleState.CurrHoverDoc != null && boardState.DraggablePlaced.GetComponent<DocumentPrompter>()) {
                // hovering ended; placement script trigger
                using (var table = TempVarTable.Alloc())
                {
                    table.Set("documentId", puzzleState.CurrHoverDoc.Interactable.AssetName);
                    ScriptUtility.Trigger(ScriptEvents.DocumentPuzzlePromptStart, table);
                }

                // move question to specific position relative to document
                boardState.DocumentRoutine.Replace(DocumentUtility.MoveAboveRelativeToDoc(boardState.DraggablePlaced, puzzleState.CurrHoverDoc));
            }

        }
        public override void ProcessWorkForComponent(DocumentInteractable primary, DocumentPrompter secondary, float deltaTime)
        {
            base.ProcessWorkForComponent(primary, secondary, deltaTime);


        }
    }
}