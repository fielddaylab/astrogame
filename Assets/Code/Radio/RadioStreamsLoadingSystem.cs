using Astro.Audio;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Files;
using FieldDay.Systems;
using UnityEngine;
using UnityEngine.Networking;

namespace Astro.Radio {
    [SysUpdate(GameLoopPhase.PreUpdate, 10000, AllowExecutionDuringLoad = true)]
    public sealed class RadioStreamsLoadingSystem : SharedStateSystemBehaviour<RadioStreamsState> {
        public override bool HasWork() {
            return base.HasWork() && m_State.LoadQueue.Count > 0;
        }

        public override void ProcessWork(float deltaTime) {
            if (m_State.LoadQueue.TryPopFront(out RadioChannel channel)) {
                if (channel.AudioClip != null) {
                    Game.Audio.QueuePreload(channel.AudioClip);
                    channel.WaveformKey = channel.AudioClip.name;
                } else if (!string.IsNullOrEmpty(channel.AudioStream)) {
                    channel.WaveformKey = VoxWaveformTable.GenerateKey(channel.AudioStream);
                    BeginRequest(channel);
                } else {
                    Assert.True(channel.Mode == RadioChannelMode.Scripted, "Non-scripted audio channel '{0}' does not have audio!", channel.name);
                }
            }
        }

        private void BeginRequest(RadioChannel channel) {
            FileLoadRequest loadRequest;
            loadRequest.Callback = OnStreamLoadFinished;
            loadRequest.CallbackContext = null;
            loadRequest.Mode = FileBufferMode.AudioClip;
            loadRequest.Flags = FileLoadFlags.Audio_Compressed;
            loadRequest.Identifier = channel.AssetId;
            loadRequest.Path = channel.AudioStream;
            loadRequest.Location = FileLocation.Streaming;
            loadRequest.Group = "RadioStream";

            Game.Files.RequestFile(loadRequest, FileLoadPriority.High);
        }

        static private void OnStreamLoadFinished(FileLoadRequest request, FileLoadResult result, object context) {
            StringHash32 assetId = request.Identifier;
            if (result.Succeeded()) {
                Log.Msg("[RadioStreamsLoadingSystem] Loaded radio clip '{0}'", result.Request.url);
                Find.State<RadioStreamsState>().DownloadedAudioClips.Add(request.Identifier, result.ReadAudioClip());
            } else {
                Log.Error("[RadioStreamsLoadingSystem] Unable to load radio stream from '{0}': {1}", result.Request.url, result.Request.error);
            }
        }
    }
}