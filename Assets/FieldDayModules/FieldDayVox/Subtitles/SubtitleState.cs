using BeauUtil;
using FieldDay.SharedState;

namespace FieldDay.Vox {
    /// <summary>
    /// Subtitle display information.
    /// </summary>
    public struct SubtitleDisplayData {
        public VoxRequestHandle VoxHandle;
        public VoxPriority Priority;
        
        public StringHash32 CharacterId;
        public StringHash32 Tag;
        
        public SubtitleEntry Subtitle;
    }

    static public partial class SubtitleUtility {
        static public readonly CastableEvent<SubtitleDisplayData> OnDisplayRequested = new CastableEvent<SubtitleDisplayData>(1);
        static public readonly CastableEvent<SubtitleDisplayData> OnDismissRequested = new CastableEvent<SubtitleDisplayData>(1);

        static public void RequestDisplay(SubtitleDisplayData data) {
            OnDisplayRequested.Invoke(data);
        }

        static public void RequestDismiss(SubtitleDisplayData data) {
            OnDismissRequested.Invoke(data);
        }
    }
}