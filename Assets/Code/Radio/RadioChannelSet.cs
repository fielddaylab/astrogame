using System;
using BeauUtil;
using EasyAssetStreaming;
using FieldDay.Assets;
using UnityEngine;

namespace Astro.Radio {
    [CreateAssetMenu(menuName = "Astro/Radio Channel Set")]
    public sealed class RadioChannelSet : NamedAsset {
        public RadioChannel[] Channels;
    }

    static public partial class RadioUtility {
        static public RadioChannel FindClosestChannel(RadioChannelSet channelSet, BitSet32 disabledMask, int frequency, out float normalizedStrength) {
            RadioChannel closestChannel = null;
            float closestNorm = 1f;
            for(int i = 0; i < channelSet.Channels.Length; i++) {
                if (disabledMask.IsSet(i)) {
                    continue;
                }

                RadioChannel channel = channelSet.Channels[i];
                int dist = Math.Abs(frequency - channel.Frequency);
                float normDist = (float) dist / channel.TuningRange;
                if (normDist <= 1 && normDist < closestNorm) {
                    closestChannel = channel;
                    closestNorm = normDist;
                }
            }

            normalizedStrength = 1f - closestNorm;
            return closestChannel;
        }
    }
}