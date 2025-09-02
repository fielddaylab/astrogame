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
        
        public string CharacterNameOverride;
        public SubtitleEntry Subtitle;
    }

    /// <summary>
    /// Subtitle dismiss information.
    /// </summary>
    public struct SubtitleDismissData {
        public VoxRequestHandle VoxHandle;
        public StringHash32 CharacterId;
        public StringHash32 Tag;

        public SubtitleDismissData(in SubtitleDisplayData display) {
            VoxHandle = display.VoxHandle;
            CharacterId = display.CharacterId;
            Tag = display.Tag;
        }
    }

    static public partial class SubtitleUtility {
        static public readonly CastableEvent<SubtitleDisplayData> OnDisplayRequested = new CastableEvent<SubtitleDisplayData>(1);
        static public readonly CastableEvent<SubtitleDismissData> OnDismissRequested = new CastableEvent<SubtitleDismissData>(1);

        static public void RequestDisplay(SubtitleDisplayData data) {
            OnDisplayRequested.Invoke(data);
        }

        static public void RequestDismiss(SubtitleDismissData data) {
            OnDismissRequested.Invoke(data);
        }
    }
}