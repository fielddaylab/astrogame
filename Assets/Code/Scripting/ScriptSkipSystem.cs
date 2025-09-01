using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.UnscaledLateUpdate, 16, ~AstroGame.PauseUpdateMask)]
    public sealed class ScriptSkipSystem : SharedStateSystemBehaviour<ScriptSkipState> {
        public override void ProcessWork(float deltaTime) {
            if (Game.Input.IsKeyDown(KeyCode.P) || Game.Input.IsKeyDown(KeyCode.Return) || Game.Input.IsKeyDown(KeyCode.M)) {
                switch(m_State.State) {
                    case ScriptSkipState.KeyState.Unheld: {
                        m_State.State = ScriptSkipState.KeyState.Pressed;
                        m_State.Timer = 0;
                        Log.Msg("[ScriptSkipSystem] Skipping dialogue line...");
                        ScriptUtility.ForEachThread(SkipLine);
                        break;
                    }
                    case ScriptSkipState.KeyState.Pressed: {
                        m_State.Timer += deltaTime;
                        if (m_State.Timer >= 1) {
                            m_State.State = ScriptSkipState.KeyState.Held;
                            m_State.Timer = 0;
                            Log.Msg("[ScriptSkipSystem] Skipping dialogue line...");
                            ScriptUtility.ForEachThread(SkipLine);
                        }
                        break;
                    }
                    case ScriptSkipState.KeyState.Held: {
                        m_State.Timer += deltaTime;
                        if (m_State.Timer >= 0.1f) {
                            m_State.State = ScriptSkipState.KeyState.Held;
                            m_State.Timer = 0;
                            Log.Msg("[ScriptSkipSystem] Skipping dialogue line...");
                            ScriptUtility.ForEachThread(SkipLine);
                        }
                        break;
                    }
                }
            } else {
                m_State.State = ScriptSkipState.KeyState.Unheld;
            }
        }

        private void SkipLine(ScriptThread thread) {
            thread.SkipCurrentVox();
        }
    }
}