using System;
using Astro.Audio;
using FieldDay;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    [SysUpdate(GameLoopPhase.LateUpdate, 504)]
    public sealed class RadioVoxSmoothingSystem : SharedStateSystemBehaviour<RadioWaveformState, RadioRig, RadioStreamsState> {
        public override void ProcessWork(float deltaTime) {
            if (m_StateA.LastVoiceCooldown > 0) {
                if (!m_StateB.VoxStaticAudioHandle.IsValid) {
                    m_StateB.VoxStaticAudioHandle = Sfx.PlayFrom("Loop.Radio.VoxStatic", m_StateB.VoxStaticEmitter);
                }
            } else {
                if (m_StateB.VoxStaticAudioHandle.IsValid) {
                    Sfx.Stop(m_StateB.VoxStaticAudioHandle, 0.2f);
                    m_StateB.VoxStaticAudioHandle = default;
                }
            }
        }
    }
}