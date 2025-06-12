using BeauUtil;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Common scripting methods.
    /// </summary>
    static internal class ScriptCommands {
        [LeafMember("DispatchEvent")]
        static internal void LeafDispatchEvent(StringHash32 eventId) {
            Game.Events.Dispatch(eventId);
        }

        [LeafMember("QueueEvent")]
        static internal void LeafQueueEvent(StringHash32 eventId) {
            Game.Events.Queue(eventId);
        }
    }
}