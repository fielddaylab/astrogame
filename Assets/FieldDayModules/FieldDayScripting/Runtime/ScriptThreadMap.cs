using System.Collections.Generic;
using BeauUtil;
using Leaf.Runtime;

namespace FieldDay.Scripting {
    /// <summary>
    /// Map of actor ids ("who") to threads.
    /// </summary>
    public sealed class ScriptThreadMap {
        /// <summary>
        /// Map of actor ids to script thread handles.
        /// </summary>
        public readonly Dictionary<StringHash32, LeafThreadHandle> Threads;

        public ScriptThreadMap(int capacity) {
            Threads = MapUtils.Create<StringHash32, LeafThreadHandle>(capacity);
        }

        /// <summary>
        /// Returns the current thread for the given actor id.
        /// </summary>
        public ScriptThread GetThread(StringHash32 targetId) {
            if (Threads.TryGetValue(targetId, out LeafThreadHandle handle)) {
                return handle.GetThread<ScriptThread>();
            }

            return null;
        }
    }
}