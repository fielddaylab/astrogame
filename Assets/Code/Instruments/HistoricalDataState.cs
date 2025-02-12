using FieldDay.SharedState;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Astro {
    public class HistoricalDataState : SharedStateComponent {
        public List<PatternMaterialPair> PatternMaterials;
    }

    [Serializable]
    public struct PatternMaterialPair {
        public HistoricalPatternType Pattern;
        public Material Material;
    }
}