
using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.PauseUpdateMask)]
    public class PauseMenuSystem : SystemBehaviour {
        public override void ProcessWork(float deltaTime) {
            if (Game.Input.IsKeyPressed(KeyCode.Tab)) {
                PauseUtility.StartTogglePause(Find.State<PauseMenuState>());
            }
        }
    }
}