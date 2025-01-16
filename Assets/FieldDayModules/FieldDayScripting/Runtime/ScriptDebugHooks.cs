#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Text;
using BeauUtil.Debugger;
using FieldDay.Debugging;

namespace FieldDay.Scripting {
    static public class ScriptDebugHooks {
        [DebugMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Scripting", 16);

            menu.AddButton("Dump All Named Script Objects", DumpAllNamedActors);

            return menu;
        }

        static public void DumpAllNamedActors() {
            StringBuilder sb = new StringBuilder(1024);
            sb.AppendFormat("[ScriptDebugHooks] Listing all {0} named ScriptActors (of {1} total)", ScriptUtility.Runtime.Actors.NamedActors.Count, ScriptUtility.Runtime.Actors.AllActors.Count);
            foreach (var actorName in ScriptUtility.Runtime.Actors.NamedActors.Keys) {
                sb.AppendFormat("\n - '{0}'", actorName.ToDebugString());
            }
            Log.Msg(sb.ToString());
        }
    }

    public enum ScriptDebugFlags {
        LogNodeEvaluation
    }
}