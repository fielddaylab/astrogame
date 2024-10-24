using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class HistoricalPatternAsset : NamedAsset {
        public HistoricalPatternType Type;
        public float WaveAmplitude;

        // TODO: Custom data
    }

    public enum HistoricalPatternType {
        Constant,
        SineWave,
        TriangularWave,
        Custom
    }
}