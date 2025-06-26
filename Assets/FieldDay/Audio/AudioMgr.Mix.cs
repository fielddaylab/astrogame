#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using BeauUtil;
using FieldDay.Filters;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Mix Data

        private unsafe struct MixData {
            public StringHash32 Id;
            public float Mix;
            public float TargetMix;
            public float TargetApproachRate;
            public AudioMixBlock Block;
        }

        #endregion // Mix Data

        #region Updates

        private unsafe void GenerateMixStateData(AudioMixState mix) {
            AudioMixBlock block;
            block.Reset();

            foreach (var busBlock in mix.Mixes) {
                int busIdx = FindBusIndexForId(busBlock.Bus);
                if (busIdx >= 0) {
                    block.Volume[busIdx] = busBlock.Volume;
                    block.Pitch[busIdx] = busBlock.Pitch;
                    block.Pan[busIdx] = busBlock.Pan;
#if SUPPORTS_AUDIOEFFECTS
                    block.LoPass[busIdx] = busBlock.LoPass;
                    block.HiPass[busIdx] = busBlock.HiPass;
#endif // SUPPORTS_AUDIOEFFECTS
                }
            }

            mix.MixBlock = block;
            mix.Linked = true;
        }

        private void UpdateMixers(float deltaTime) {
            for (int i = m_ActiveMixStates.Count; i-- > 0;) {
                ref MixData mixData = ref m_ActiveMixStates[i];
                float val = mixData.Mix;
                float delta = mixData.TargetMix - val;
                val = Mathf.MoveTowards(val, mixData.TargetMix, mixData.TargetApproachRate * deltaTime);
                mixData.Mix = val;

                if (val > 0) {
                    ApplyMixToBuses(mixData.Block, val);
                } else if (mixData.TargetMix <= 0) {
                    m_ActiveMixStates.FastRemoveAt(i);
                }
            }
        }

        private unsafe void ApplyMixToBuses(in AudioMixBlock block, float mix) {
            for (int i = 0; i < m_BusCount; i++) {
                ref AudioPropertyBlock busBlock = ref m_WorkingBusProperties[i];
                busBlock.Volume *= AudioMixBlock.MixMultiplier(block.Volume[i], mix);
                busBlock.Pitch *= AudioMixBlock.MixMultiplier(block.Pitch[i], mix);
                busBlock.Pan += block.Pan[i] * mix;
#if SUPPORTS_AUDIOEFFECTS
                busBlock.LoPass += block.LoPass[i] * mix;
                busBlock.HiPass += block.HiPass[i] * mix;
#endif // SUPPORTS_AUDIOEFFECTS
            }
        }

        #endregion // Updates

        private void SetMixStateTarget(StringHash32 mixId, float mixValue, float transitionDuration, bool proportionalTransition, bool useDefaultEnvelope) {
            if (mixId.IsEmpty) {
                return;
            }

            mixValue = Mathf.Clamp01(mixValue);

            AudioMixState mixAsset = Find.NamedAsset<AudioMixState>(mixId);

            int mixIndex = -1;
            for (int i = m_ActiveMixStates.Count; i-- > 0;) {
                if (m_ActiveMixStates[i].Id == mixId) {
                    mixIndex = i;
                    break;
                }
            }

            if (mixIndex < 0) {
                if (mixValue <= 0) {
                    return;
                }

                mixIndex = m_ActiveMixStates.Count;

                m_ActiveMixStates.PushBack(new MixData() {
                    Id = mixId,
                    Block = mixAsset.MixBlock,
                    Mix = 0,
                    TargetMix = 0,
                    TargetApproachRate = 0
                });
            }

            ref MixData data = ref m_ActiveMixStates[mixIndex];

            if (transitionDuration <= 0 && useDefaultEnvelope) {
                transitionDuration = data.Mix < mixValue ? mixAsset.DefaultEnvelope.Attack : mixAsset.DefaultEnvelope.Decay;
            }

            if (transitionDuration <= 0) {
                data.Mix = data.TargetMix = mixValue;
                data.TargetApproachRate = 0;

                if (mixValue <= 0) {
                    m_ActiveMixStates.FastRemoveAt(mixIndex);
                }
            } else {
                data.TargetMix = mixValue;
                data.TargetApproachRate = (proportionalTransition ? Math.Abs(data.TargetMix - data.Mix) : 1) / transitionDuration;
            }
        }
    }
}