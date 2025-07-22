using System;
using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    [SysUpdate(GameLoopPhase.Update, 500)]
    public sealed class RadioStaticSystem : SharedStateSystemBehaviour<RadioRig> {
        const float FadeInDuration = 1;
        const float FadeOutDuration = 2;
        const float WaitDuration = 1;

        public override void ProcessWork(float deltaTime) {
            if (m_State.InstantaneousTune) {
                m_State.InstantaneousTune = false;
                m_State.StaticMode = RadioStaticMode.Off;
                m_State.StaticVolume = 0;
                m_State.StaticModeTimer = 0;
            } else {    
                // Special case for off channel
                if (m_State.Dial.CurrentValue == 100) {
                    if (m_State.StaticMode != RadioStaticMode.Off) {
                        Sfx.PlayDetached("Oneshot.LabButtonC.Click", m_State.StreamEmitter.transform);
                    }
                    TurnOffRadio(m_State);
                    return;
                }

                switch (m_State.StaticMode) {
                    case RadioStaticMode.Off: {
                        UpdateOff(m_State);
                        break;
                    }

                    case RadioStaticMode.FadeIn: {
                        UpdateFadeIn(m_State, deltaTime);
                        break;
                    }

                    case RadioStaticMode.Tuning: {
                        UpdateTuning(m_State, deltaTime);
                        break;
                    }

                    case RadioStaticMode.FadeOut: {
                        UpdateFadeOut(m_State, deltaTime);
                        break;
                    }
                }
            }

            if (Sfx.IsActive(m_State.StaticAudioHandle)) {
                float vol = m_State.StaticVolume * (1 - m_State.NormalizedChannelStrength);
                Sfx.SetVolume(m_State.StaticAudioHandle, vol);
            }

            float duckingMix = Math.Max(m_State.StaticVolume / 2, m_State.NormalizedChannelStrength);
            Sfx.SetMixStateTarget(m_State.MixId, duckingMix);
        }

        static private void UpdateOff(RadioRig state) {
            if (!state.Dial.Updated) return;

            state.StaticAudioHandle = Sfx.PlayFrom("Loop.Radio.Static", state.StaticEmitter, new SfxPlayArgs() { Pitch = 1, Volume = 0 });
            state.StaticVolume = 0;

            state.StaticMode = RadioStaticMode.FadeIn;
            state.StaticModeTimer = 0;
        }

        static private void UpdateFadeIn(RadioRig state, float deltaTime) {
            state.StaticModeTimer += deltaTime;
            state.StaticVolume = Math.Min(1, state.StaticModeTimer / FadeInDuration);

            if (state.StaticVolume >= 1) {
                state.StaticMode = RadioStaticMode.Tuning;
                state.StaticModeTimer = 0;
            }
        }

        static private void UpdateFadeOut(RadioRig state, float deltaTime) {
            if (state.Dial.Updated) {
                state.StaticMode = RadioStaticMode.FadeIn;
                state.StaticModeTimer = state.StaticVolume * FadeInDuration;
            } else {
                state.StaticModeTimer += deltaTime;
                state.StaticVolume = 1 - Math.Min(1, state.StaticModeTimer / FadeOutDuration);

                if (state.StaticVolume <= 0) TurnOffRadio(state);
            }
        }

        static private void TurnOffRadio(RadioRig rig) {
            rig.StaticMode = RadioStaticMode.Off;
            rig.StaticVolume = 0;
            rig.StaticModeTimer = 0;
            Sfx.Stop(rig.StaticAudioHandle);
            rig.StaticAudioHandle = default;
        }

        static private void UpdateTuning(RadioRig state, float deltaTime) {
            if (state.Dial.Updated || (state.NormalizedChannelStrength > 0 && state.NormalizedChannelStrength < 0.85f)) {
                state.StaticModeTimer = 0;
            } else {
                state.StaticModeTimer += deltaTime;
                if (state.StaticModeTimer >= WaitDuration) {
                    state.StaticMode = RadioStaticMode.FadeOut;
                    state.StaticModeTimer = 0;
                }
            }
        }
    }
}