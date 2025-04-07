using BeauUtil;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class DocumentPuzzleState : SharedStateComponent
    {
        [NonSerialized] public bool PuzzleActive = false;
        [NonSerialized] public DocumentPuzzleAsset CurrPuzzle = null;
        [NonSerialized] public DocumentRenderer CurrHoverDoc = null;
    }

    public static partial class DocumentUtility
    {
        #region Spawning

        private static void SpawnQuestionDocument(StringHash32 id)
        {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id), id);
        }

        #endregion // Spawning

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
                SpawnQuestionDocument(puzzleState.CurrPuzzle.QuestionAsset.AssetId);
            }

            puzzleState.PuzzleActive = true;
        }

        public static void EndDocumentPuzzle(DocumentPuzzleState state)
        {
            state.PuzzleActive = false;
        }

        #endregion // Sequence

        public static void UpdatePuzzleHoverAsset(DocumentPuzzleState state, DocumentRenderer doc)
        {
            if (doc == null) {
                if (state.CurrHoverDoc != null) {
                    Debug.Log("[PuzzleState] Hover ended");
                    state.CurrHoverDoc = doc;
                }
            }
            else if (state.CurrHoverDoc != null && state.CurrHoverDoc.Interactable.AssetName.Equals(doc.Interactable.AssetName)) {
                // no change in hover asset
                return;
            }
            else {
                // change in hover asset
                Debug.Log("[PuzzleState] Hover changed");
                state.CurrHoverDoc = doc;
            }
        }
    }
}