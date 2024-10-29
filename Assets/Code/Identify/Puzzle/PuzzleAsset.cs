using System;
using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Puzzle Asset")]
    public sealed class PuzzleAsset : NamedAsset {
        [Serializable]
        public struct Row {
            [AssetName(typeof(CelestialAsset))] public StringHash32 Object;
            public DataTypeMask ProvidedProperties;
        }
        
        #region Inspector

        public string DisplayName;
        public string[] HintText;

        [Header("Data")]
        public DataTypeMask RequiredProperties;
        public Row[] Rows;

        #endregion // Inspector
    }

    public static partial class PuzzleUtility
    {
        public static int NumCols(PuzzleAsset puzzle)
        {
            int count = 0;

            if ((puzzle.RequiredProperties & DataTypeMask.Name) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Coordinates) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Color) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.ApparentMagnitude) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.AbsoluteMagnitude) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.MaterialSpectrum) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Temperature) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Distance) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Coordinates) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_ApparentMagnitude) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Temperature) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Distance) != 0) { count++; }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Color) != 0) { count++; }

            return count;
        }
    }
}