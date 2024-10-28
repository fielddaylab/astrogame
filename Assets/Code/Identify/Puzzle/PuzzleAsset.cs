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
}