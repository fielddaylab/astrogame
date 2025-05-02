using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.SharedState;
using FieldDay.Systems;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    public sealed class RadioStreamsTimeSystem : SharedStateSystemBehaviour<RadioStreamsState> {
        public override void ProcessWork(float deltaTime) {
            for(int i = m_State.VirtualStreams.Count - 1; i >= 0; i--) {
                ref RadioVirtualStream stream = ref m_State.VirtualStreams[i];
                stream.Time += deltaTime;
                if (stream.Duration > 0 && stream.Time >= stream.Duration) {
                    switch (stream.Mode) {
                        case RadioChannelMode.OneShot: {
                            KillVirtualStream(stream.ChannelId, true);
                            m_State.VirtualStreams.FastRemoveAt(i);
                            break;
                        }
                        default: {
                            stream.Time -= stream.Duration;
                            break;
                        }
                    }
                }
            }
        }

        private void KillVirtualStream(StringHash32 channelId, bool disableOneShot) {
            int channelIndex = Array.IndexOf(m_State.ChannelIndexMap, channelId);
            Assert.True(channelIndex >= 0);
            if (disableOneShot) {
                m_State.DeactivatedChannels.Set(channelIndex);
            }
        }
    }
}