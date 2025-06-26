using BeauUtil;
using System;
using UnityEngine;
using UnityEngine.Playables;


#if USING_TIMELINE
using UnityEngine.Timeline;
#endif // USING_TIMELINE

#if USING_TIMELINE

using MarkerTypeIndex = BeauUtil.TypeIndex<FieldDay.Animation.Timeline.CustomTimelineMarker>;

namespace FieldDay.Animation.Timeline {
    static public class TimelineExtensions {
        #region Marker Handlers

        static private readonly CastableAction<CustomTimelineMarker>[] s_MarkerHandlers = new CastableAction<CustomTimelineMarker>[MarkerTypeIndex.Capacity];

        static public void RegisterMarkerHandler<T>(Action<T> markerHandler) where T : CustomTimelineMarker {
            s_MarkerHandlers[MarkerTypeIndex.Get<T>()].Set(markerHandler);
        }

        #endregion // Marker Handlers
    }
}

#endif // USING_TIMELINE