#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using BeauUtil.Debugger;
using FieldDay.Debugging;

namespace FieldDay.Scripting {
    static public class ScriptDebugHooks {
#if DEVELOPMENT
        [DebugMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Leaf");

            DebugFlags.Menu.AddFlagToggle(menu, "Verbose Node Evaluation", ScriptDebugFlags.LogNodeEvaluation);

            return menu;
        }
#endif // DEVELOPMENT
    }

    public enum ScriptDebugFlags {
        LogNodeEvaluation
    }
}