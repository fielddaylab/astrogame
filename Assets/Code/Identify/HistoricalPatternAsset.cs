using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Historical Pattern")]
    public sealed class HistoricalPatternAsset : NamedAsset {
        public HistoricalPatternType Type;
        public float WaveAmplitude;

        // TODO: Custom data
    }

    public enum HistoricalPatternType {
        None = 0,
        Constant,
        SineWave,
        TriangularWave,
        Parallax,
        Custom
    }
}