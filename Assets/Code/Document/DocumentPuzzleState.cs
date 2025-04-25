using BeauUtil;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using UnityEngine;

namespace Astro {
    public class DocumentPuzzleState : SharedStateComponent {
        [NonSerialized] public bool PuzzleActive = false;
        [NonSerialized] public DocumentPuzzleAsset CurrPuzzle = null;
        [NonSerialized] public DocumentRenderer CurrHoverDoc = null;

        public enum DebuggingFlags {
            DisplayDocumentHoverInfo
        }
    }

    public static partial class DocumentUtility {
        #region Leaf

        [LeafMember("StartDocumentPuzzle")]
        private static void LeafStartDocumentPuzzle(StringHash32 id) {
            var puzzleState = Find.State<DocumentPuzzleState>();
            var viewState = Find.State<ViewState>();

            var puzzleAsset = Find.NamedAsset<DocumentPuzzleAsset>(id);

            StartDocumentPuzzle(puzzleState, viewState, puzzleAsset);
        }

        #endregion // Leaf

        #region Sequence

        public static void StartDocumentPuzzle(DocumentPuzzleState puzzleState, ViewState viewState, DocumentPuzzleAsset puzzleAsset) {
            // Set current puzzle
            puzzleState.CurrPuzzle = puzzleAsset;
            if (puzzleAsset) {
                StringHash32 questionId = puzzleState.CurrPuzzle.QuestionAsset.AssetId;
                DocumentBoardState state = Find.State<DocumentBoardState>();
                ArchiveState archiveState = Find.State<ArchiveState>();
                SpawnDocumentToCamera(state, archiveState, questionId);
            }

            puzzleState.PuzzleActive = true;
            GameLoop.ResumeUpdates(AstroGame.DocumentUpdateMask);
        }

        public static void EndDocumentPuzzle(DocumentPuzzleState state)
        {
            state.PuzzleActive = false;
            GameLoop.SuspendUpdates(AstroGame.DocumentUpdateMask);
        }

        #endregion // Sequence

        public static void UpdatePuzzleHoverAsset(DocumentPuzzleState state, DocumentRenderer doc) {
            if (doc == null) {
                if (state.CurrHoverDoc != null) {
                    if (Game.IsDevBuild){
                        if (DebugFlags.IsFlagSet(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo)) {
                            Debug.Log("[DocumentUtility > UpdatePuzzleHoverAsset] Hover ended.");
                        }
                    }
                    state.CurrHoverDoc = doc;
                }
            }
            else if (state.CurrHoverDoc != null && state.CurrHoverDoc.Interactable.AssetName.Equals(doc.Interactable.AssetName)) {
                // no change in hover asset
                if (Game.IsDevBuild){
                    if (DebugFlags.IsFlagSet(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo)) {
                        Debug.Log("[DocumentUtility > UpdatePuzzleHoverAsset] Hover unchanged: " + doc.name);
                    }
                }
                return;
            } else {
                // change in hover asset
                if (Game.IsDevBuild){
                    if (DebugFlags.IsFlagSet(DocumentPuzzleState.DebuggingFlags.DisplayDocumentHoverInfo)) {
                        Debug.Log("[PuzzleState] Hover changed: " + doc.name);
                    }
                }
                state.CurrHoverDoc = doc;
            }
        }
    }
}