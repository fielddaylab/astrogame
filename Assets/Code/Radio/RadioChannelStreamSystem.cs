using System;
using UnityEngine;

using BeauUtil;
using BeauUtil.Debugger;

using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;
using FieldDay.Scripting;

namespace Astro.Radio {
    public sealed class RadioChannelStreamSystem : SharedStateSystemBehaviour<RadioStreamsState, RadioRig> {
        public override void ProcessWork(float deltaTime) {
            RadioChannel channel = m_StateB.ClosestChannel;

            if (m_StateA.LastKnownChannel != channel) {
                if (Sfx.IsActive(m_StateB.StreamAudioHandle)) {
                    RadioUtility.MakeVirtual(m_StateB.StreamEmitter, m_StateA.LastKnownChannel, m_StateA);
                    Sfx.Stop(m_StateB.StreamAudioHandle);
                    m_StateB.StreamAudioHandle = default;
                }

                m_StateA.LastKnownChannel = channel;

                if (channel != null) {
                    AudioClip clip = channel.AudioClip;
                    if (clip == null) {
                        if (!m_StateA.DownloadedAudioClips.TryGetValue(channel.AssetId, out clip)) {
                            clip = channel.Mode == RadioChannelMode.Scripted ? null : m_StateA.FallbackClip;
                        }
                    }

                    if (clip != null) {
                        StringHash32 eventId = channel.EventOverride;
                        if (eventId.IsEmpty) {
                            eventId = channel.Mode == RadioChannelMode.OneShot ? m_StateA.OneshotEvent: m_StateA.LoopedEvent;
                        }

                        m_StateB.StreamAudioHandle = Sfx.PlayFrom(eventId, clip, m_StateB.StreamEmitter, new SfxPlayArgs() {
                            Pitch = 1,
                            Volume = 0
                        });

                        bool canRestore = RadioUtility.TryMakeReal(channel, m_StateA, out RadioVirtualStream virtualStream);
                        if (canRestore) {
                            Log.Msg("[RadioChannelStreamSystem] Restoring channel '{0}' from time {1}", channel.AssetId.ToDebugString(), virtualStream.Time);
                            Sfx.Seek(m_StateB.StreamAudioHandle, virtualStream.Time);
                        } else if (channel.Mode != RadioChannelMode.OneShot) {
                            float time = RNG.Instance.NextFloat(clip.length / 2);
                            Log.Msg("[RadioChannelStreamSystem] Starting channel '{0}' from random time {1}", channel.AssetId.ToDebugString(), time);
                            Sfx.Seek(m_StateB.StreamAudioHandle, time);
                        } else {
                            Log.Msg("[RadioChannelStreamSystem] Starting channel '{0}'", channel.AssetId.ToDebugString());
                        }
                    }
                }
            }

            if (Sfx.IsActive(m_StateB.StreamAudioHandle)) {
                Sfx.SetVolume(m_StateB.StreamAudioHandle, m_StateB.NormalizedChannelStrength);
            } else if (m_StateA.WasAnyChannelPlayingLastFrame) {
                if (m_StateA.LastKnownChannel != null && m_StateA.LastKnownChannel.Mode == RadioChannelMode.OneShot) {
                    int channelIndex = Array.IndexOf(m_StateA.ChannelIndexMap, m_StateA.LastKnownChannel.AssetId);
                    Assert.True(channelIndex >= 0);
                    m_StateA.DeactivatedChannels.Set(channelIndex);
                    m_StateB.LastKnownFrequency = -1;
                }

                var listenState = Find.State<RadioListenState>();

                if (listenState.CurrListenChannel != null) {
                    // channel was playing but has since stopped (only applies to one-shots)
                    using (var table = TempVarTable.Alloc()) {
                        table.Set("wasPlayerListening", true);
                        ScriptUtility.Trigger(ScriptEvents.RadioChannelFinished, table);
                    }
                }
            }

            m_StateA.WasAnyChannelPlayingLastFrame = Sfx.IsActive(m_StateB.StreamAudioHandle);
        }
    }
}