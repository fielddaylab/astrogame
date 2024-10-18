using System.Collections;
using BeauUtil;
using BeauUtil.Variants;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting {
    static public class DefaultLeaf {
        static public class KeyPairs {
            static public readonly TableKeyPair SceneName = TableKeyPair.Parse("scene:name");
            static public readonly TableKeyPair TimeNow = TableKeyPair.Parse("time:now");
        }

        static internal void ConfigureDefaultVariables(CustomVariantResolver resolver) {
            resolver.SetVar(KeyPairs.SceneName, LeafGetSceneName);
            resolver.SetVar(KeyPairs.TimeNow, () => Time.time);
        }

        static private Variant LeafGetSceneName() {
            return Game.Scenes.MainScene().Name;
        }

        [LeafMember("HasSeen")]
        static private bool LeafSeenNode(StringHash32 nodeId) {
            var historyBuff = ScriptUtility.Runtime.CurrentHistoryBuffer;
            if (historyBuff != null) {
                return historyBuff.HasSeen(nodeId, ScriptNodeMemoryScope.Persistent);
            } else {
                return false;
            }
        }

        [LeafMember("TriggerResponse")]
        static private void TriggerResponse([BindContext] LeafEvalContext ctx, StringHash32 triggerId, StringHash32 targetId = default) {
            ScriptThread thread = (ScriptThread) ctx.Thread;
            
            if (thread != null && targetId == "this") {
                targetId = thread.Target();
            }

            ScriptUtility.Trigger(triggerId, targetId, thread?.Actor, thread?.Locals);
        }

        [LeafMember("TriggerResponseAndWait")]
        static private IEnumerator TriggerResponseAndWait([BindContext] LeafEvalContext ctx, StringHash32 triggerId, StringHash32 targetId = default) {
            ScriptThread thread = (ScriptThread) ctx.Thread;

            if (thread != null && targetId == "this") {
                targetId = thread.Target();
            }

            var response = ScriptUtility.Trigger(triggerId, targetId, thread?.Actor, thread?.Locals);
            if (response.IsRunning()) {
                return response.Wait();
            } else {
                return null;
            }
        }

        [LeafMember("SecondsSince")]
        static private Variant LeafSecondsSince(float since, float expected = 0) {
            float elapsed = Time.time - since;
            if (expected >= 0) {
                return elapsed >= expected;
            } else {
                return elapsed;
            }
        }
    }
}