using BeauUtil;
using UnityEngine;


#if USING_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif // USING_TIMELINE

#if USING_TIMELINE

namespace FieldDay.Animation.Timeline {
    [NonIndexed]
    [TypeIndexCapacity(256)]
    public abstract class CustomTimelineMarker : Marker, INotification {
        public virtual PropertyName id { get; }
    }
}

#endif // USING_TIMELINE