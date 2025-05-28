using BeauUtil;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    public class DocumentPuzzleState : SharedStateComponent {
        [NonSerialized] public bool PuzzleActive = false;
        [NonSerialized] public DocumentPuzzleAsset CurrPuzzle = null;
        [NonSerialized] public DocumentRenderer CurrHoverDoc = null;

        [SerializeField] private Color32 m_docHighlightColor;
        [SerializeField] public static Color32 DocHighlightColor;

        public Dictionary<StringHash32, StringHash32> CurrSolutionPairs = new Dictionary<StringHash32, StringHash32>();

        public enum DebuggingFlags {
            DisplayDocumentHoverInfo
        }

        private void Awake() {
           DocHighlightColor = m_docHighlightColor; 
        }
    }

    public static partial class DocumentUtility {
        #region Leaf

        [LeafMember("StartDocumentPuzzle")]
        private static void LeafStartDocumentPuzzle(StringHash32 id) {
            var puzzleState = Find.State<DocumentPuzzleState>();
            var viewState = Find.State<ViewState>();

            var puzzleAsset = Find.NamedAsset<DocumentPuzzleAsset>(id);

            puzzleState.CurrSolutionPairs.Clear();

            StartDocumentPuzzle(puzzleState, viewState, puzzleAsset);
        }

        [LeafMember("IsDocumentPuzzleSolved")]
        private static bool LeafIsDocumentPuzzleSolved()
        {
            var puzzleState = Find.State<DocumentPuzzleState>();
            return DocumentUtility.IsDocPuzzleCorrect(puzzleState);
        }

        #endregion // Leaf

        #region Sequence

        public static void StartDocumentPuzzle(DocumentPuzzleState puzzleState, ViewState viewState, DocumentPuzzleAsset puzzleAsset) {
            // Set current puzzle
            puzzleState.CurrPuzzle = puzzleAsset;

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
                    SetDocumentHighlight(state.CurrHoverDoc, Color.white);
                    state.CurrHoverDoc = null;
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
                SetDocumentHighlight(state.CurrHoverDoc, Color.white);
                state.CurrHoverDoc = doc;
                SetDocumentHighlight(state.CurrHoverDoc, DocumentPuzzleState.DocHighlightColor);
            }
        }

        public static void UpdateDocPuzzleAnswer(DocumentPuzzleState puzzleState, StringHash32 questionId, StringHash32 answerId)
        {
            if (!puzzleState.CurrSolutionPairs.ContainsKey(questionId)) {
                puzzleState.CurrSolutionPairs.Add(questionId, answerId);
            }
            else {
                puzzleState.CurrSolutionPairs[questionId] = answerId;
            }
        }

        public static bool IsDocPuzzleCorrect(DocumentPuzzleState puzzleState)
        {
            foreach (var pair in puzzleState.CurrPuzzle.SolutionPairs) {
                if (!puzzleState.CurrSolutionPairs.ContainsKey(pair.Question.AssetId)) {
                    return false;
                }
                else if (!puzzleState.CurrSolutionPairs[pair.Question.AssetId].Equals(pair.Answer.AssetId))
                {
                    return false;
                }
            }
            return true;
        }

        public static void SetDocumentHighlight(DocumentRenderer doc, Color color) {
            if (doc == null) return;

            StreamingQuadTexture[] textures = doc.GetComponentsInChildren<StreamingQuadTexture>(true);
            foreach (var texture in textures) {
                texture.Color = color;
            }
            ColorGroup[] cgs = doc.GetComponentsInChildren<ColorGroup>(true);
            foreach (var cg in cgs) {
                cg.Color = color;
            }
        }
    }
}