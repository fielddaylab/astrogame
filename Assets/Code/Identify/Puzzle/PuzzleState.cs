using BeauPools;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class PuzzleState : SharedStateComponent {
        public PuzzleAsset QueuedPuzzle;
        public PuzzleAsset ActivePuzzle;

        public int SelectedRow = -1;
        public bool RowUpdated = false;

        public GameObject RowCellPrefab;

        // public Pool RowPool;
    }

    public static partial class PuzzleUtility
    {
        public static bool TrySetSelectedRow(PuzzleState state, int index)
        {
            if (state.ActivePuzzle == null) { return false; }
            if (index < 0) { index = state.ActivePuzzle.Rows.Length - 1; }

            index = index % state.ActivePuzzle.Rows.Length;
            state.SelectedRow = index;
            state.RowUpdated = true;

            return true;
        }
    }
}