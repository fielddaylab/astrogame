using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Common scripting methods.
    /// </summary>
    static internal class ScriptCommands {
        #region Events

        [LeafMember("DispatchEvent")]
        static internal void LeafDispatchEvent(StringHash32 eventId) {
            Game.Events.Dispatch(eventId);
        }

        [LeafMember("QueueEvent")]
        static internal void LeafQueueEvent(StringHash32 eventId) {
            Game.Events.Queue(eventId);
        }

        #endregion // Events

        #region Signals

        [LeafMember("Signal")]
        static internal void LeafDispatchSignal(StringHash32 eventId, Variant argument = default) {
            ScriptUtility.Runtime.SignalMap.Dispatch(eventId, argument);
        }

        [LeafMember("QueueSignal")]
        static internal void LeafQueueSignal(StringHash32 eventId, Variant argument = default) {
            ScriptUtility.Runtime.SignalMap.Queue(eventId, argument);
        }

        #endregion // Signals

        #region Input

        [LeafMember("InputPushPause")]
        static internal void LeafPushInputPause() {
            Game.Input.PauseRaycasts();
            Game.Input.PauseDevices();
        }

        [LeafMember("InputPopPause")]
        static internal void LeafPopInputPause() {
            Game.Input.ResumeRaycasts();
            Game.Input.ResumeDevices();
        }

        #endregion // Input
    }
}