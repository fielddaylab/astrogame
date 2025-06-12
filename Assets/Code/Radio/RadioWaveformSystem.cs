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
    public sealed class RadioWaveformSystem : SharedStateSystemBehaviour<RadioWaveformState, RadioRig, RadioStreamsState> {
        static private int VoiceLerpPropertyId;
        static private int LineColorPropertyId;
        static private int DataScaleYPropertyId;

        public override void ProcessWork(float deltaTime) {
            bool wasPlayingVox = false;
            foreach(var vox in m_StateA.Voices) {
                if (vox.Player.isPlaying) {
                    SubtitleStyle style = Find.NamedAsset<SubtitleStyle>(vox.CharacterId);
                    m_StateA.CurrentColor = style.WaveformColor;
                    m_StateA.CurrentLerp = 1;

                    float voxAmp = 1;
                    if (m_StateA.VoxWaveformTable.TryFind(VoxUtility.CurrentLineCode(vox), out var waveform)) {
                        float time = VoxUtility.CurrentPlaybackPosition(vox);
                        float duration = vox.Player.clip.length;
                        voxAmp = VoxWaveform.ReadAmplitude(waveform, time, duration);
                    }

                    m_StateA.CurrentScale = m_StateA.VoiceScale * voxAmp;

                    wasPlayingVox = true;
                    break;
                }
            }

            if (!wasPlayingVox) {
                float noiseComponent = m_StateB.StaticVolume;
                float channelComponent = m_StateB.NormalizedChannelStrength;

                float channelAmp = 0;
                if (m_StateB.ClosestChannel != null && m_StateB.StreamEmitter.isPlaying) {
                    channelAmp = 1;
                    if (m_StateA.RadioWaveformTable.TryFind(m_StateB.ClosestChannel.WaveformKey, out var waveform)) {
                        float time = m_StateB.StreamEmitter.time;
                        float duration = m_StateB.StreamEmitter.clip.length;
                        channelAmp = VoxWaveform.ReadAmplitude(waveform, time, duration);
                    }
                }

                m_StateA.CurrentLerp = channelComponent;
                m_StateA.CurrentScale = (noiseComponent * (1 - channelComponent) * m_StateA.NoiseScale)
                    + (channelComponent * channelAmp) * m_StateA.ChannelScale;

                m_StateA.CurrentColor = Color.LerpUnclamped(m_StateA.NoiseColor, m_StateA.ChannelColor, channelComponent);
            }

            Material targetMat = m_StateA.WaveformMaterial;
            targetMat.SetFloat(VoiceLerpPropertyId, m_StateA.CurrentLerp);
            targetMat.SetFloat(DataScaleYPropertyId, m_StateA.CurrentScale);
            targetMat.SetColor(LineColorPropertyId, m_StateA.CurrentColor);
        }

        public override void Initialize() {
            VoiceLerpPropertyId = Shader.PropertyToID("_VoiceLerp");
            LineColorPropertyId = Shader.PropertyToID("_LineColor");
            DataScaleYPropertyId = Shader.PropertyToID("_DataScaleY");
        }
    }
}