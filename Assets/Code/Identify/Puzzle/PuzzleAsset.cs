using System;
using System.Collections.Generic;
using System.Linq;
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

        [Serializable]
        public struct Edge {
            [AssetName(typeof(CelestialAsset))] public StringHash32 Object1;
            [AssetName(typeof(CelestialAsset))] public StringHash32 Object2;
        }
        
        #region Inspector

        public string DisplayName;
        public string[] ClueText;

        [Header("Data")]
        public DataTypeMask RequiredProperties;
        public Row[] Rows;  

        [Header("Constellation")]
        public Edge[] Edges;

        [Header("Puzzle Position")]
        public EqCoords PuzzleCoordinates;
        public float PuzzleCameraZoom;

        #endregion // Inspector
    }

    public static partial class PuzzleUtility
    {
        public static void ExtractCols(PuzzleAsset puzzle, out List<DataTypeMask> types, out int numCols)
        {
            numCols = 0;
            types = new List<DataTypeMask>();

            if ((puzzle.RequiredProperties & DataTypeMask.Name) != 0) {
                types.Add(DataTypeMask.Name);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Coordinates) != 0) {
                types.Add(DataTypeMask.Coordinates);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Color) != 0) {
                types.Add(DataTypeMask.Color);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.ApparentMagnitude) != 0) { 
                types.Add(DataTypeMask.ApparentMagnitude);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.AbsoluteMagnitude) != 0) { 
                types.Add(DataTypeMask.AbsoluteMagnitude);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.MaterialSpectrum) != 0) {
                types.Add(DataTypeMask.MaterialSpectrum);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Temperature) != 0) {
                types.Add(DataTypeMask.Temperature);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Distance) != 0) { 
                types.Add(DataTypeMask.Distance);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Coordinates) != 0) { 
                types.Add(DataTypeMask.Historical_Coordinates);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                types.Add(DataTypeMask.Historical_ApparentMagnitude);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Temperature) != 0) { 
                types.Add(DataTypeMask.Historical_Temperature);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Distance) != 0) {
                types.Add(DataTypeMask.Historical_Distance);
            }
            if ((puzzle.RequiredProperties & DataTypeMask.Historical_Color) != 0) { 
                types.Add(DataTypeMask.Historical_Color);
            }

            numCols = types.Count;
        }
    }
}
