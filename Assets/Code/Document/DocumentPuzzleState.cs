using BeauUtil;
using FieldDay;
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
    }

    public static partial class DocumentUtility
    {
        #region Spawning

        private static void SpawnQuestionDocument(StringHash32 id)
        {
            SpawnDocument(Find.NamedAsset<DocumentAsset>(id));

            // TODO: additional question config here
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
            // Shift focus to document board
            var targetNode = ViewNavUtility.GetNodeById("Left");
            ViewNavUtility.MoveToNode(viewState, targetNode);

            // TODO: lock focus

            // Set current puzzle
            puzzleState.CurrPuzzle = puzzleAsset;
            if (puzzleAsset) {
                SpawnQuestionDocument(puzzleState.CurrPuzzle.QuestionAsset.AssetId);

                // TODO: start dragging
            }

            puzzleState.PuzzleActive = true;
        }

        public static void EndDocumentPuzzle(DocumentPuzzleState state)
        {
            state.PuzzleActive = false;
        }

        #endregion // Sequence
    }
}