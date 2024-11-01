using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;

namespace Astro
{
    public class PuzzlePools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class PuzzleCellPool : SerializablePool<PuzzleCell> { }

        #endregion // Types

        [Header("Puzzle")]
        public PuzzleCellPool Cells;

        [Header("Shared")]
        public Transform PoolRoot;

        void IRegistrationCallbacks.OnRegister()
        {
            Cells.TryInitialize(PoolRoot);
        }

        void IRegistrationCallbacks.OnDeregister()
        {

        }
    }
}