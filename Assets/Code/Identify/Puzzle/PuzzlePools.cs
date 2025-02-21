using BeauPools;
using FieldDay.SharedState;
using FieldDay;
using System;
using UnityEngine;
using TMPro;
using BeauUtil;
using System.Collections.Generic;

namespace Astro
{
    public class PuzzlePools : SharedStateComponent, IRegistrationCallbacks
    {
        #region Types

        [Serializable] public class PuzzleCellPool : SerializablePool<PuzzleCell> { }
        [Serializable] public class PuzzleColorCellPool : SerializablePool<PuzzleCell> { }
        [Serializable] public class PuzzleHeaderPool : SerializablePool<PuzzleHeader> { }

        #endregion // Types

        [Header("Puzzle")]
        public PuzzleCellPool Cells;
        public PuzzleColorCellPool ColorCells;
        public PuzzleHeaderPool Headers;

        [Header("Shared")]
        public Transform PoolRoot;

        [Header("Allocations")]
        public int NumXSmall = 8;
        public int NumSmall = 8;
        public int NumMedium = 8;
        public int NumLarge = 8;

        [NonSerialized] public Dictionary<PuzzleCellLibrary.BundleType, int> Allocations;

        void IRegistrationCallbacks.OnRegister()
        {
            Cells.TryInitialize(PoolRoot);
            ColorCells.TryInitialize(PoolRoot);
            Headers.TryInitialize(PoolRoot);
            Allocations = new Dictionary<PuzzleCellLibrary.BundleType, int>() {
                { PuzzleCellLibrary.BundleType.XSmall, 0 },
                { PuzzleCellLibrary.BundleType.Small, 0 },
                { PuzzleCellLibrary.BundleType.Medium, 0 },
                { PuzzleCellLibrary.BundleType.Large, 0 },
            };
        }

        void IRegistrationCallbacks.OnDeregister()
        {

        }
    }

    public static class PuzzlePoolUtility
    {
        public static void ClearAllocations(PuzzlePools pools)
        {
            pools.Allocations[PuzzleCellLibrary.BundleType.XSmall] = 0;
            pools.Allocations[PuzzleCellLibrary.BundleType.Small] = 0;
            pools.Allocations[PuzzleCellLibrary.BundleType.Medium] = 0;
            pools.Allocations[PuzzleCellLibrary.BundleType.Large] = 0;
        }

        public static bool TryAllocateOnBundleType(PuzzlePools pools, PuzzleCellLibrary.BundleType type, out SerializedHash32 id)
        {
            int compareNum = pools.NumXSmall;
            switch (type)
            {
                case PuzzleCellLibrary.BundleType.XSmall:
                    compareNum = pools.NumXSmall;
                    break;
                case PuzzleCellLibrary.BundleType.Small:
                    compareNum = pools.NumSmall;
                    break;
                case PuzzleCellLibrary.BundleType.Medium:
                    compareNum = pools.NumMedium;
                    break;
                case PuzzleCellLibrary.BundleType.Large:
                    compareNum = pools.NumLarge;
                    break;
                default:
                    break;
            }

            if (pools.Allocations[type] < compareNum)
            {
                // allocations available
                pools.Allocations[type]++;

                id = ((int)type).ToStringLookup() + "C" + pools.Allocations[type].ToStringLookup();
            }
            else
            {
                // no more allocations available
                Debug.LogError("[PuzzlePools] No more puzzle cell allocations available for type " + type + "!");
                id = "0" + "C" + "0";
                return false;
            }

            return true;
        }
    }
}