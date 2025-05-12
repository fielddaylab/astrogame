using System;
using FieldDay;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    [SysUpdate(GameLoopPhase.Update, 501)]
    public sealed class RadioTuningSystem : SharedStateSystemBehaviour<RadioRig, RadioStreamsState> {
        public override void ProcessWork(float deltaTime) {
            DayConfigAsset dayConfig = DayConfigUtil.GetConfigForState();
            RadioChannelSet channelSet = dayConfig.RadioChannels;

            int val = m_StateA.Dial.CurrentValue;
            if (val != m_StateA.LastKnownFrequency) {
                m_StateA.LastKnownFrequency = val;

                RadioChannel channel = RadioUtility.FindClosestChannel(channelSet, m_StateB.DeactivatedChannels, val, out float normStrength);
                m_StateA.NormalizedChannelStrength = normStrength;

                if (m_StateA.ClosestChannel != channel) {
                    m_StateA.ClosestChannel = channel;
                }
            }
        }
    }
}