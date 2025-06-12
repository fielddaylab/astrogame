using Astro.Audio;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
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
            if (m_State.CurrentLoadRequest != null) {
                if (!m_State.CurrentLoadRequest.isDone) {
                    return;
                }

                RadioChannel queuedLoad = m_State.LoadQueue.PopFront();

                if (m_State.CurrentLoadRequest.result != UnityWebRequest.Result.Success) {
                    Log.Error("[RadioStreamsLoadingSystem] Unable to load radio stream from '{0}': {1}", m_State.CurrentLoadRequest.url, m_State.CurrentLoadRequest.error);
                } else {
                    DownloadHandlerAudioClip handler = (DownloadHandlerAudioClip) m_State.CurrentLoadRequest.downloadHandler;
                    m_State.DownloadedAudioClips.Add(queuedLoad.AssetId, handler.audioClip);
                    Log.Msg("[RadioStreamsLoadingSystem] Loaded radio clip '{0}'", m_State.CurrentLoadRequest.url);
                }

                m_State.CurrentLoadRequest.Dispose();
                m_State.CurrentLoadRequest = null;
            }

            if (m_State.LoadQueue.TryPeekFront(out RadioChannel channel)) {
                if (channel.AudioClip != null) {
                    Game.Audio.QueuePreload(channel.AudioClip);
                    channel.WaveformKey = channel.AudioClip.name;
                    m_State.LoadQueue.PopFront();
                } else if (!string.IsNullOrEmpty(channel.AudioStream)) {
                    channel.WaveformKey = VoxWaveformTable.GenerateKey(channel.AudioStream);
                    BeginRequest(channel);
                } else {
                    Assert.True(channel.Mode == RadioChannelMode.Scripted, "Non-scripted audio channel '{0}' does not have audio!", channel.name);
                    m_State.LoadQueue.PopFront();
                }
            }
        }

        private void BeginRequest(RadioChannel channel) {
            string url = Streaming.ResolveAddressToURL(channel.AudioStream);
            UnityWebRequest uwr = UnityWebRequest.Get(url);
            DownloadHandlerAudioClip handler = new DownloadHandlerAudioClip(url, AudioType.UNKNOWN);
            handler.streamAudio = false;
            handler.compressed = true;
            uwr.downloadHandler = handler;
            uwr.SendWebRequest();

            m_State.CurrentLoadRequest = uwr;
        }
    }
}