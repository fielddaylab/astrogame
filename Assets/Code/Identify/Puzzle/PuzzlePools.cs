using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;
using TMPro;

namespace Astro
{
    public class PuzzlePools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class PuzzleCellPool : SerializablePool<PuzzleCell> { }
        [Serializable] public class PuzzleHeaderPool : SerializablePool<PuzzleHeader> { }

        #endregion // Types

        [Header("Puzzle")]
        public PuzzleCellPool Cells;
        public PuzzleHeaderPool Headers;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister()
        {
            Cells.TryInitialize(PoolRoot);
            Headers.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister()
        {

        }
    }
}