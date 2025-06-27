using BeauUtil;
using FieldDay.Assets;
using System.Text;

namespace Astro.Radio {
    public sealed class RadioSubtitlesFile : NamedAsset {

    }

    public struct RadioSubtitleEntry {
        public UnsafeSpan<RadioSubtitleChunk> Chunks;
        public RadioSubtitlesFile File;
    }

    public struct RadioSubtitleChunk {
        public float StartTime;
        public float Duration;
        public StringHash32 CharacterId;
        public int StringIndex;
    }

    public struct RadioSubtitleOutput {
        public string Text;
        public StringHash32 CharacterId;
    }

    public struct RadioSubtitleReader {
        public int ChunkMarker;
        public float TimeMarker;

        static public readonly RadioSubtitleReader Default = new RadioSubtitleReader() {
            ChunkMarker = -1,
            TimeMarker = -1
        };
    }

    static public partial class RadioUtility {
        //static public bool ReadSubtitle(ref RadioSubtitleReader reader, RadioSubtitleEntry entry, float time, ref RadioSubtitleOutput output) {
            
        //}
    }
}