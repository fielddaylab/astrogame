using BeauUtil;
using UnityEngine;
using System.ComponentModel;


#if USING_TIMELINE
using FieldDay.Animation.Timeline;
#endif // USING_TIMELINE

#if USING_TIMELINE
namespace FieldDay.Scripting.Timeline {
    [DisplayName("Script Trigger")]
    public sealed class ScriptTrigger : CustomTimelineMarker {
        public SerializedHash32 TriggerId;
    }
}
#endif // USING_TIMELINE