using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Astro.Audio;
using Astro.Save;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scenes;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using UnityEngine;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Astro {
    public sealed class AstroGame : Game {
        
        public const int OpenSubmissionUpdateMask = 1 << 0;
        public const int DocumentUpdateMask = 1 << 1;
        public const int InstrumentUpdateMask = 1 << 2;
        public const int MonitorControlsUpdateMask = 1 << 3;
        public const int InteractUpdateMask = 1 << 4;
        public const int PuzzleSubmissionUpdateMask = 1 << 5;
        public const int AnySubmissionUpdateMask = 1 << 6;
        public const int PauseUpdateMask = 1 << 7;

        /// <summary>
        /// Save state buffer.
        /// </summary>
        static public SaveMgr SaveBuffer { get; private set; }

        static public new EventDispatcher<EvtArgs> Events { get; private set; }

        [DebugMenuFactory]
        private static DMInfo LoadLevel() {
            StoryAsset story = Find.GlobalAsset<StoryAsset>();

            DMInfo info = new DMInfo("Progress");
            info.AddButton("NextLevel", () => {
                ScriptUtility.KillAllThreads();
                ScriptTriggers.LoadNextDay();
            });

            info.AddDivider();

#if DEVELOPMENT
            foreach(var dayId in story.Days){
                RegisterDayLoadButton(info, dayId);
            }

            info.AddDivider();

            RegisterDecoderLoadButton(info, "Day5");

            if (story.DEBUG_SandboxDay) {
                info.AddDivider();
                info.AddButton("Load Sandbox", () => {
                    ScriptUtility.KillAllThreads();
                    MusicUtility.StopMusic();
                    Game.Events.Dispatch(GameEvents.BeforeNextDayLoad);
                    Log.Msg("[ScriptTriggers] Loading sandbox day");
                    Find.State<PlayerProgressState>().LoadDebugScene = true;
                    Game.Scenes.LoadMainScene(story.DEBUG_SandboxDay.Scene, true);
                });
            }
#endif // DEVELOPMENT
            return info;
        }

#if DEVELOPMENT
        static private void RegisterDayLoadButton(DMInfo menu, StringHash32 dayId) {
            menu.AddButton("Load " + Find.NamedAsset<DayConfigAsset>(dayId).name, () => {
                ScriptUtility.KillAllThreads();
                ScriptTriggers.LoadDay(dayId);
                MusicUtility.StopMusic();

                Find.State<PlayerProgressState>().LoadDebugScene = false;
            });
        }
#endif // DEVELOPMENT


        static private void RegisterDecoderLoadButton(DMInfo menu, StringHash32 dayId)
        {
            menu.AddButton("Begin Decode (Day 5)", () => {
                ScriptUtility.KillAllThreads();

                ScriptUtility.Trigger(ScriptEvents.BeginDecoderSequence);

#if DEVELOPMENT
                Find.State<PlayerProgressState>().LoadDebugScene = false;
#endif // DEVELOPMENT
            });
        }

        [InvokePreBoot]
        static private void OnPreBoot() {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);

            SaveBuffer = new SaveMgr();

            PlayerProgressState progress = new PlayerProgressState();
            SharedState.Register(progress);


            Rendering.EnableAspectClamping(4, 3);

            Game.Scenes.RegisterLoadDependency(new ScriptPreloadDependency());

            //GameLoop.OnDebugUpdate.Register(() => {
            //    using(var psb = PooledStringBuilder.Create()) {
            //        psb.Builder.Append("Frame #: ").AppendNoAlloc(Frame.Index);
            //        DebugDraw.AddViewportText(new Vector2(0.5f, 1), new Vector2(0, -8), psb, Color.yellow, 0, TextAnchor.UpperCenter, DebugTextStyle.BackgroundDarkOpaque);
            //    }
            //});
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            GameLoop.OnPreUpdate.Register(() => {
                Physics.SyncTransforms();
            });

            Scenes.OnMainSceneLateEnable.Register(() => {
                ScriptUtility.Invoke("ScenePreload");
            });

            Scenes.OnMainSceneReady.Register(() => {
                Find.GuiModule<LoadingIcon>().Hide();
                ScriptUtility.Trigger("SceneReady");
            });

            Scenes.OnMainSceneUnloaded.Register(() => {
                Find.GuiModule<LoadingIcon>().Show();
            });
        }

        private class ScriptPreloadDependency : ISceneLoadDependency {
            public bool IsLoaded(SceneLoadPhase loadPhase) {
                if (loadPhase == SceneLoadPhase.BeforeReady) {
                    return ScriptUtility.CurrentThreadCount == 0;
                }
                return true;
            }
        }
    }
}