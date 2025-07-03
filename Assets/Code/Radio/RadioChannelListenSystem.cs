using System;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    /// <summary>
    /// Tracks whether the player is listening to any channel
    /// </summary>
    public sealed class RadioChannelListenSystem : SharedStateSystemBehaviour<RadioStreamsState, RadioRig, RadioListenState> {
        public override void ProcessWork(float deltaTime) {
            // if the player is tuned-in to a specific channel, increase the listen timer
            if (m_StateB.NormalizedChannelStrength >= m_StateC.ListenStrengthThreshold) {
                m_StateC.ListenTime += deltaTime;
            }
            else if (m_StateC.ListenTime != 0) {
                m_StateC.ListenTime = 0;
            }

            // if the player has been listening for long enough, register this channel as being listened to
            if (m_StateC.CurrListenChannel == null && m_StateC.ListenTime >= m_StateC.ListenTimeThreshold) {
                m_StateC.CurrListenChannel = m_StateA.LastKnownChannel;

                using (var table = TempVarTable.Alloc()) {
                    if (m_StateC.CurrListenChannel != null) table.Set("channelId", m_StateC.CurrListenChannel.AssetId);
                    ScriptUtility.Trigger(ScriptEvents.RadioChannelListenStart, table);
                }
            }
            // otherwise this channel is no longer being listened to
            else if (m_StateC.CurrListenChannel != null && m_StateC.ListenTime < m_StateC.ListenTimeThreshold) {
                var channelId = m_StateC.CurrListenChannel.AssetId;
                m_StateC.CurrListenChannel = null;

                using (var table = TempVarTable.Alloc()) {
                    table.Set("channelId", channelId);
                    ScriptUtility.Trigger(ScriptEvents.RadioChannelListenEnd, table);
                }
            }
        }
    }
}