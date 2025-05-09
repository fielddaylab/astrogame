using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Astro.Audio;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.UI.Animation;
using UnityEngine;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Astro {
    public sealed class AstroGame : Game {
        
        public const int SubmissionUpdateMask = 1 << 0;
        public const int DocumentUpdateMask = 1 << 1;
        public const int InstrumentUpdateMask = 1 << 2;
        public const int MonitorControlsUpdateMask = 1 << 3;
        public const int InteractUpdateMask = 1 << 4;

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

            foreach(var dayId in story.Days){
                RegisterDayLoadButton(info, dayId);
            }

#if DEVELOPMENT
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

        static private void RegisterDayLoadButton(DMInfo menu, StringHash32 dayId) {
            menu.AddButton("Load " + Find.NamedAsset<DayConfigAsset>(dayId).name, () => {
                ScriptUtility.KillAllThreads();
                ScriptTriggers.LoadDay(dayId);
                MusicUtility.StopMusic();

#if DEVELOPMENT
                Find.State<PlayerProgressState>().LoadDebugScene = false;
#endif // DEVELOPMENT
            });
        }

        [InvokePreBoot]
        static private void OnPreBoot() {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);
            
            PlayerProgressState progress = new PlayerProgressState();
            SharedState.Register(progress);

            Rendering.EnableAspectClamping(4, 3);

            //GameLoop.OnDebugUpdate.Register(() => {
            //    using(var psb = PooledStringBuilder.Create()) {
            //        psb.Builder.Append("Frame #: ").AppendNoAlloc(Frame.Index);
            //        DebugDraw.AddViewportText(new Vector2(0.5f, 1), new Vector2(0, -8), psb, Color.yellow, 0, TextAnchor.UpperCenter, DebugTextStyle.BackgroundDarkOpaque);
            //    }
            //});
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            Scenes.OnMainSceneReady.Register(() => {
                ScriptUtility.Trigger("SceneReady");
            });
        }
    }
}