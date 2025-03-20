using System;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.Scripting;
using FieldDay.SharedState;

namespace Astro {
    public sealed class AstroGame : Game {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }

        [DebugMenuFactory]
        private static DMInfo LoadLevel() {
            DMInfo info = new DMInfo("Progress");
            info.AddButton("NextLevel", () => {
                ScriptUtility.KillAllThreads();
                ScriptTriggers.LoadNextDay();
            });
            return info;
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
    
    public sealed class PlayerProgressState : ISharedState {
        public int DayIndex = 0;
        [NonSerialized] public List<ArchiveLayout> DayLayouts = new List<ArchiveLayout>();
    }
}