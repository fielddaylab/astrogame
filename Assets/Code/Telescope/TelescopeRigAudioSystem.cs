using System;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.SharedState;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 11)]
    public sealed class TelescopeRigAudioSystem : SharedStateSystemBehaviour<TelescopeRigAudio, TelescopeRig> {
        public override void ProcessWork(float deltaTime) {
            bool movedDome = !Mathf.Approximately(m_StateA.LastKnownRotation.x, m_StateB.LastAppliedRotation.x);
            bool movedTelescope = movedDome || !Mathf.Approximately(m_StateA.LastKnownRotation.y, m_StateB.LastAppliedRotation.y);

            m_StateA.LastKnownRotation = m_StateB.LastAppliedRotation;

            if (movedDome) {
                if (!m_StateA.DomeAudioHandle.IsValid) {
                    m_StateA.DomeAudioHandle = Sfx.Play(m_StateA.DomeRotationLoop, m_StateA.DomeRotationLoopLocation);
                    Sfx.SetVolume(m_StateA.DomeAudioHandle, 0);
                    Sfx.SetVolume(m_StateA.DomeAudioHandle, 1, 0.1f);
                    //Log.Msg("beginning dome move audio");
                }
            } else {
                if (m_StateA.DomeAudioHandle.IsValid) {
                    Sfx.Stop(m_StateA.DomeAudioHandle, 0.15f);
                    m_StateA.DomeAudioHandle = default;
                    //Log.Msg("stopping dome move audio");
                }
            }

            if (movedTelescope) {
                if (!m_StateA.BaseAudioHandle.IsValid) {
                    m_StateA.BaseAudioHandle = Sfx.Play(m_StateA.BaseRotationLoop, m_StateA.BaseRotationLoopLocation);
                    Sfx.SetVolume(m_StateA.BaseAudioHandle, 0);
                    Sfx.SetVolume(m_StateA.BaseAudioHandle, 1, 0.1f);
                    //Log.Msg("beginning telescope move audio");
                }
            } else {
                if (m_StateA.BaseAudioHandle.IsValid) {
                    Sfx.Stop(m_StateA.BaseAudioHandle, 0.15f);
                    m_StateA.BaseAudioHandle = default;
                    //Log.Msg("stopping telescope move audio");
                }
            }
        }
    }
}