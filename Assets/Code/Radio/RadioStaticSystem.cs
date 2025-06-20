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

            if (Sfx.IsActive(m_State.StaticAudioHandle)) {
                float vol = m_State.StaticVolume * (1 - m_State.NormalizedChannelStrength);
                Sfx.SetVolume(m_State.StaticAudioHandle, vol);

                //using (var psb = PooledStringBuilder.Create()) {
                //    psb.Builder.Append("Radio Static volume: ").AppendNoAlloc(vol, 3);
                //    DebugDraw.AddLogText(psb, Color.green);
                //}
            }

            float duckingMix = Math.Max(m_State.StaticVolume / 2, m_State.NormalizedChannelStrength);
            Sfx.SetMixStateTarget(m_State.MixId, duckingMix);
        }

        static private void UpdateOff(RadioRig state) {
            if (state.InstantaneousTune) {
                state.InstantaneousTune = false;
                state.StaticVolume = 0;
                state.StaticMode = RadioStaticMode.FadeIn;
                state.StaticModeTimer = 0;
                state.StaticModeTimer = 0;
                return;
            }

            if (state.Dial.Updated) {
                state.StaticAudioHandle = Sfx.PlayFrom("Loop.Radio.Static", state.StaticEmitter, new SfxPlayArgs() { Pitch = 1, Volume = 0 });
                state.StaticVolume = 0;

                state.StaticMode = RadioStaticMode.FadeIn;
                state.StaticModeTimer = 0;
            }
        }

        static private void UpdateFadeIn(RadioRig state, float deltaTime) {
            if (state.InstantaneousTune) {
                state.InstantaneousTune = false;
                state.StaticMode = RadioStaticMode.Tuning;
                state.StaticModeTimer = 0;
                return;
            }

            state.StaticModeTimer += deltaTime;
            state.StaticVolume = Math.Min(1, state.StaticModeTimer / FadeInDuration);

            if (state.StaticVolume >= 1) {
                state.StaticMode = RadioStaticMode.Tuning;
                state.StaticModeTimer = 0;
            }
        }

        static private void UpdateFadeOut(RadioRig state, float deltaTime) {
            if (state.InstantaneousTune) {
                state.InstantaneousTune = false;
                state.StaticMode = RadioStaticMode.Off;
                state.StaticModeTimer = 0;
                Sfx.Stop(state.StaticAudioHandle);
                state.StaticAudioHandle = default; 
                return;
            }

            if (state.Dial.Updated) {
                state.StaticMode = RadioStaticMode.FadeIn;
                state.StaticModeTimer = state.StaticVolume * FadeInDuration;
            } else {
                state.StaticModeTimer += deltaTime;
                state.StaticVolume = 1 - Math.Min(1, state.StaticModeTimer / FadeOutDuration);

                if (state.StaticVolume <= 0) {
                    state.StaticMode = RadioStaticMode.Off;
                    state.StaticModeTimer = 0;
                    Sfx.Stop(state.StaticAudioHandle);
                    state.StaticAudioHandle = default;
                }
            }
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