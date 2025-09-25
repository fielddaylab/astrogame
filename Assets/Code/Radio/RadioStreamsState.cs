using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using static BeauUtil.ColorGroup;

namespace Astro.Radio {
    public sealed class RadioStreamsState : SharedStateComponent, IScenePreload, ISceneLoadDependency, IRegistrationCallbacks {
        [AudioEvent] public StringHash32 OneshotEvent;
        [AudioEvent] public StringHash32 LoopedEvent;
        public AudioClip FallbackClip;

        [NonSerialized] public BitSet32 DeactivatedChannels;
        [NonSerialized] public RingBuffer<RadioVirtualStream> VirtualStreams = new RingBuffer<RadioVirtualStream>(8, RingBufferMode.Expand);

        [NonSerialized] public RadioChannel LastKnownChannel;

        [NonSerialized] public bool WasAnyChannelPlayingLastFrame;

        // Loading

        [NonSerialized] public StringHash32[] ChannelIndexMap;
        [NonSerialized] public RingBuffer<RadioChannel> LoadQueue = new RingBuffer<RadioChannel>(8, RingBufferMode.Expand);
        [NonSerialized] public Dictionary<StringHash32, AudioClip> DownloadedAudioClips = MapUtils.Create<StringHash32, AudioClip>(8);

        bool ISceneLoadDependency.IsLoaded(SceneLoadPhase loadPhase) {
            return loadPhase != SceneLoadPhase.BeforeReady || LoadQueue.Count == 0;
        }

        void IRegistrationCallbacks.OnDeregister() {
            foreach(var asset in DownloadedAudioClips.Values) {
                AssetUtility.ManualUnload(asset);
            }
            DownloadedAudioClips.Clear();
            Game.Files.CancelRequestsInGroup("RadioStream");

            Game.Scenes.DeregisterLoadDependency(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Scenes.RegisterLoadDependency(this);
        }

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            DayConfigAsset dayConfig = DayConfigUtil.GetConfigForState();
            RadioChannelSet channels = dayConfig.RadioChannels;
            Assert.NotNullOrDestroyed(channels);

            ChannelIndexMap = ArrayUtils.MapFrom(channels.Channels, (a) => a.AssetId);

            int idx = 0;
            foreach(var channel in channels.Channels) {
                LoadQueue.PushBack(channel);
                DeactivatedChannels.Set(idx++, channel.StartInactive);
            }

            return null;
        }
    }

    public struct RadioVirtualStream {
        public StringHash32 ChannelId;
        public float Time;
        public float Duration;
        public RadioChannelMode Mode;
    }

    static public partial class RadioUtility {
        static public void MakeVirtual(AudioSource stream, RadioChannel channel, RadioStreamsState streams) {
            RadioVirtualStream virt;
            virt.ChannelId = channel.AssetId;
            virt.Time = stream.time;
            virt.Duration = stream.clip.length;
            virt.Mode = channel.Mode;

            int idx = streams.VirtualStreams.FindIndex(FindVirtualStreamWithId, channel.AssetId);
            if (idx >= 0) {
                streams.VirtualStreams[idx] = virt;
            } else {
                streams.VirtualStreams.PushBack(virt);
            }
        }

        static public bool TryMakeReal(RadioChannel channel, RadioStreamsState streams, out RadioVirtualStream virtualStream) {
            int idx = streams.VirtualStreams.FindIndex(FindVirtualStreamWithId, channel.AssetId);
            if (idx >= 0) {
                virtualStream = streams.VirtualStreams[idx];
                streams.VirtualStreams.FastRemoveAt(idx);
                return true;
            }

            virtualStream = default;
            return false;
        }

        static private Predicate<RadioVirtualStream, StringHash32> FindVirtualStreamWithId = (t, i) => t.ChannelId == i;

        [LeafMember("SetChannelActive")]
        public static void SetChannelActive(StringHash32 channelId, bool active) {
            var streamState = Find.State<RadioStreamsState>();
            var radioRig = Find.State<RadioRig>();

            int channelIndex = Array.IndexOf(streamState.ChannelIndexMap, channelId);
            Assert.True(channelIndex >= 0, "No channel with id '{0}' found in channl set", channelId);

            if (active) {
                streamState.DeactivatedChannels.Unset(channelIndex);
            } else {
                streamState.DeactivatedChannels.Set(channelIndex);
            }

            // refresh radio as if just tuning into the current frequency (resets in RadioTuningSystem)
            radioRig.LastKnownFrequency = -1;
        }
    }
}