#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System.Text;
using UnityEngine;

using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.Vox;

#if DEVELOPMENT
namespace FieldDay.Scripting {
    static public class ScriptDebugHooks {
        [DebugMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Scripting", 16);

            DebugFlags.Menu.AddFlagToggle(menu, "Verbose Evaluation", ScriptDebugFlags.LogNodeEvaluation);
            DebugFlags.Menu.AddFlagToggle(menu, "Display Stats", ScriptDebugFlags.DisplayThreadStats);
            DebugFlags.Menu.AddFlagToggle(menu, "Display Details", ScriptDebugFlags.DisplayThreadDetails);
            menu.AddDivider();

            menu.AddButton("Dump All Named Script Objects", DumpAllNamedActors);

            return menu;
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            GameLoop.OnDebugUpdate.Register(DebugUpdate);

            SmokeTestMgr.RegisterResetHandler(() => {
                ScriptUtility.KillAllThreads();
            });

            SceneMgr.RegisterDebugLoadCallback(() => {
                ScriptUtility.KillAllThreads();
            });
        }

        static private void DebugUpdate() {
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                ScriptRuntimeState runtime = ScriptUtility.Runtime;
                ScriptDatabase db = ScriptUtility.DB;

                if (DebugFlags.IsFlagSet(ScriptDebugFlags.DisplayThreadStats)) {
                    psb.Builder.Append("Leaf Stats:\n   ")
                        .AppendNoAlloc(runtime.ActiveThreads.Count).Append(" active threads");
                    psb.Builder.Append("\n   ").AppendNoAlloc(runtime.Actors.AllActors.Count).Append(" actors (").AppendNoAlloc(runtime.Actors.NamedActors.Count).Append(" named)");
                    psb.Builder.Append("\n   ").AppendNoAlloc(db.RegisteredPackages.Count).Append(" leaf packages");
                    psb.Builder.Append("\n   ").AppendNoAlloc(db.LoadedNodeBuckets.Count).Append(" node buckets");

                    DebugDraw.AddLogText(psb, ColorBank.FloralWhite);

                    psb.Builder.Clear();
                }

                if (DebugFlags.IsFlagSet(ScriptDebugFlags.DisplayThreadDetails)) {
                    if (runtime.ActiveThreads.Count == 0) {
                        psb.Builder.Append("NO ACTIVE THREADS");
                    } else {
                        foreach(var threadHandle in runtime.ActiveThreads) {
                            ScriptThread thread = threadHandle.GetThread<ScriptThread>();
                            ScriptNode node = thread.PeekNode();
                            StringHash32 target = thread.Target();
                            StringHash32 initialEvt = thread.InitialTriggerOrFunction();

                            psb.Builder.Append("<color=yellow>Thread: ").Append(thread.Name);
                            if (runtime.Cutscene == threadHandle) {
                                psb.Builder.Append(" [CUTSCENE]");
                            }

                            psb.Builder.Append("</color>\nPriority ").AppendNoAlloc((int)thread.Priority()).Append("   ");

                            if (!initialEvt.IsEmpty) {
                                psb.Builder.Append("\nBucket '").Append(initialEvt.ToDebugString()).Append('\'').Append("   ");
                            }

                            if (node != null) {
                                psb.Builder.Append("\nExecuting '").Append(node.FullName).Append("'").Append("   ");
                            }

                            if (target.IsEmpty) {
                                psb.Builder.Append("\nNo owner").Append("   ");
                            } else {
                                psb.Builder.Append("\nOwned by '").Append(target.ToDebugString()).Append("'").Append("   ");
                            }

                            VoxRequestHandle vox = thread.GetCurrentVox();
                            if (VoxUtility.IsValid(vox)) {
                                StringHash32 voxLineCode = VoxUtility.GetLineCode(vox);
                                float playback = VoxUtility.GetPlaybackPosition(vox);
                                float duration = VoxUtility.GetDuration(vox);
                                psb.Builder.Append("\nPlaying vox '").Append(voxLineCode.ToDebugString()).Append("' (")
                                    .AppendNoAlloc(playback, 2).Append("/").AppendNoAlloc(duration, 2).Append(")").Append("   ");
                            }

                            psb.Builder.Append('\n');
                        }
                    }

                    psb.Builder.TrimEnd(StringUtils.DefaultNewLineChars);

                    DebugDraw.AddViewportText(new Vector2(1, 1), new Vector2(-8, -96), psb, ColorBank.FloralWhite, 0, TextAnchor.UpperRight, DebugTextStyle.Default);
                }
            }
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
        LogNodeEvaluation,
        DisplayThreadStats,
        DisplayThreadDetails,
    }
}
#endif // DEVELOPMENT