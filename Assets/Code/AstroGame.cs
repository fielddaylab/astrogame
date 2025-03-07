using System;
using BeauRoutine;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;

namespace Astro {
    public sealed class AstroGame : Game {
        static public new EventDispatcher<EvtArgs> Events { get; private set; }

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
    }
}