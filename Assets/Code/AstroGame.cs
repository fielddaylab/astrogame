using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

namespace Astro {
    public sealed class AstroGame : Game {
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
            return info;
        }

        static private void RegisterDayLoadButton(DMInfo menu, StringHash32 dayId) {
            menu.AddButton("Load " + Find.NamedAsset<DayConfigAsset>(dayId).name, () => {
                ScriptUtility.KillAllThreads();
                ScriptTriggers.LoadDay(dayId);
            });
        }

        [InvokePreBoot]
        static private void OnPreBoot() {
            Events = new EventDispatcher<EvtArgs>();
            SetEventDispatcher(Events);
            
            PlayerProgressState progress = new PlayerProgressState();
            SharedState.Register(progress);

            Rendering.EnableAspectClamping(4, 3);
        }

        [InvokeOnBoot]
        static private void OnBoot() {
            Scenes.OnMainSceneReady.Register(() => {
                ScriptUtility.Trigger("SceneReady");
            });
        }
    }
}