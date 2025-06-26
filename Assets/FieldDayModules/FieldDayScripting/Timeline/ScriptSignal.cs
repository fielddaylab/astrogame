using BeauUtil;
using UnityEngine;
using BeauUtil.Variants;
using System.ComponentModel;


#if USING_TIMELINE
using FieldDay.Animation.Timeline;
#endif // USING_TIMELINE

#if USING_TIMELINE
namespace FieldDay.Scripting.Timeline {
    [DisplayName("Script Signal")]
    public sealed class ScriptSignal : CustomTimelineMarker {
        public SerializedHash32 SignalId;
        public SerializedVariant Argument;
    }
}
#endif // USING_TIMELINE