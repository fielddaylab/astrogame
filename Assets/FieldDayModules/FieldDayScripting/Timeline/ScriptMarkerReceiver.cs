using BeauUtil;
using FieldDay.Components;
using UnityEngine;


#if USING_TIMELINE
using UnityEngine.Playables;
using UnityEngine.Timeline;
#endif // USING_TIMELINE

#if USING_TIMELINE
namespace FieldDay.Scripting.Timeline {
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class ScriptMarkerReceiver : BatchedComponent, INotificationReceiver {
        public void OnNotify(Playable origin, INotification notification, object context) {
            if (notification == null) {
                return;
            }

            ScriptSignal signal = notification as ScriptSignal;
            if (signal != null) {
                ScriptUtility.DispatchSignal(signal.SignalId, signal.Argument);
                return;
            }

            ScriptTrigger trigger = notification as ScriptTrigger;
            if (trigger != null) {
                ScriptUtility.Trigger(trigger.TriggerId);
                return;
            }
        }
    }
}
#endif // USING_TIMELINE