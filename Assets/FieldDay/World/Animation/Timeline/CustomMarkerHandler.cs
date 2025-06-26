using BeauUtil;
using FieldDay.Components;
using UnityEngine;


#if USING_TIMELINE
using UnityEngine.Playables;
#endif // USING_TIMELINE

#if USING_TIMELINE

namespace FieldDay.Animation.Timeline {
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class CustomMarkerHandler : BatchedComponent, INotificationReceiver {
        public void OnNotify(Playable origin, INotification notification, object context) {
            CustomTimelineMarker marker = notification as CustomTimelineMarker;
            if (marker != null) {

            }
        }
    }
}

#endif // USING_TIMELINE