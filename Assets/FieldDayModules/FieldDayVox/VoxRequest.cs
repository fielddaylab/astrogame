using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.SharedState;
using TinyIL;
using UnityEngine;

namespace FieldDay.Vox {
    public struct VoxRequest {
        public StringHash32 LineCode;
        public SubtitleEntry Subtitle;
        public StringHash32 CharacterId;
        
        public VoxPriority Priority;
        public bool StartPlayback;
        public bool UnloadAfterPlayback;

        public VoxEventDelegate OnSpeak;
        public VoxEventDelegate OnFinish;

        internal VoxEmitter Emitter;
        internal StringHash32 EventOverride;
        internal VoxRequestPlaybackState State;
        internal AudioClip LoadedClip;
        internal UniqueId16 Id;
    }

    public delegate void VoxEventDelegate(VoxRequestHandle handle, VoxEmitter emitter, StringHash32 lineCode);

    public enum VoxPriority : byte {
        Unassigned = 0,
        Effort,
        Chatter,
        Reaction,
        Cutscene,
    }

    internal class VoxRequestState : ISharedState {
        public RingBuffer<VoxRequest> ActiveRequests = new RingBuffer<VoxRequest>(8);
        public UniqueIdAllocator16 RequestIdGenerator = new UniqueIdAllocator16(32);

        public VoxEventDelegate OnAnyVoiceSpeak;
        public VoxEventDelegate OnAnyVoiceFinished;
    }

    internal enum VoxRequestPlaybackState {
        Queued,
        Loading,
        Loaded,
        Playing,
        Complete
    }

    static public partial class VoxUtility {
        [SharedStateReference] static internal VoxRequestState Requests { get; private set; }

        #region Request Data

        static internal VoxRequest GetRequestData(VoxRequestHandle id) {
            if (!Requests.RequestIdGenerator.IsValid(id.m_Id)) {
                return default;
            }

            var requestBuffer = Requests.ActiveRequests;
            for (int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                if (req.Id == id.m_Id) {
                    return req;
                }
            }

            return default;
        }

        static internal ref VoxRequest GetModifiableRequestData(VoxRequestHandle id) {
            if (!Requests.RequestIdGenerator.IsValid(id.m_Id)) {
                return ref Unsafe.NullRef<VoxRequest>();
            }

            var requestBuffer = Requests.ActiveRequests;
            for (int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                if (req.Id == id.m_Id) {
                    return ref req;
                }
            }

            return ref Unsafe.NullRef<VoxRequest>();
        }

        static internal bool KillVoxRequest(ref VoxRequestHandle id) {
            if (!Requests.RequestIdGenerator.IsValid(id.m_Id)) {
                id = default;
                return false;
            }

            var requestBuffer = Requests.ActiveRequests;
            for (int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                if (req.Id != id.m_Id) {
                    continue;
                }

                KillVoxRequestAtIndex(ref req, i);
                id = default;
                return true;
            }

            return false;
        }

        static internal void KillVoxRequestAtIndex(ref VoxRequest req, int index) {
            VoxEmitter emitter = req.Emitter;
            Sfx.Stop(emitter.AudioHandle);
            emitter.AudioHandle = default;

            emitter.Player.Stop();
            emitter.PlayingPriority = VoxPriority.Unassigned;

            if (req.UnloadAfterPlayback) {
                QueueUnload(req.LineCode);
            }

            VoxRequest cachedReq = req;
            VoxRequestHandle handle = new VoxRequestHandle(cachedReq.Id);

            Requests.RequestIdGenerator.Free(req.Id);
            Requests.ActiveRequests.FastRemoveAt(index);

            if (Requests.OnAnyVoiceFinished != null) {
                Requests.OnAnyVoiceFinished(handle, cachedReq.Emitter, cachedReq.LineCode);
            }

            cachedReq.Emitter.OnFinishSpeaking.Invoke(handle, cachedReq.LineCode);

            if (cachedReq.OnFinish != null) {
                cachedReq.OnFinish(handle, cachedReq.Emitter, cachedReq.LineCode);
            }

            SubtitleUtility.RequestDismiss(new SubtitleDisplayData() {
                CharacterId = cachedReq.Emitter.CharacterId,
                Subtitle = cachedReq.Subtitle,
                Priority = cachedReq.Priority,
                VoxHandle = handle
            });
        }

        #endregion // Request Data

        #region Speak

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public VoxRequestHandle Speak(VoxEmitter emitter, StringHash32 lineCode, VoxPriority priority = VoxPriority.Cutscene) {
            return Speak(emitter, lineCode, null, priority);
        }

        static public VoxRequestHandle Speak(VoxEmitter emitter, StringHash32 lineCode, string subtitleFallback, VoxPriority priority = VoxPriority.Cutscene) {
            Assert.True(emitter != null);
            return Speak(emitter, new VoxRequest() {
                LineCode = lineCode,
                Priority = priority,
                Subtitle = new SubtitleEntry(subtitleFallback),
                StartPlayback = true,
                CharacterId = emitter.CharacterId,
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public VoxRequestHandle Speak(StringHash32 emitterId, StringHash32 lineCode, VoxPriority priority = VoxPriority.Cutscene) {
            return Speak(emitterId, lineCode, null, priority);
        }

        static public VoxRequestHandle Speak(StringHash32 emitterId, StringHash32 lineCode, string subtitleFallback, VoxPriority priority = VoxPriority.Cutscene) {
            VoxEmitter emitter = FindEmitter(emitterId);
            if (emitter == null) {
                Log.Warn("[VoxUtility] No VoxEmitter found for id '{0}'", emitterId);
                return default;
            }

            return Speak(emitter, new VoxRequest() {
                LineCode = lineCode,
                Priority = priority,
                Subtitle = new SubtitleEntry(subtitleFallback),
                StartPlayback = true,
                CharacterId = emitterId
            });
        }

        static public VoxRequestHandle Speak(VoxEmitter emitter, VoxRequest request) {
            Assert.True(emitter != null);
            if (request.Priority < emitter.PlayingPriority) {
                Log.Debug("[VoxUtility] Skipping playback of lower priority vox request '{0}'", request.LineCode);
                return default;
            }

            Stop(emitter);

            request.Emitter = emitter;
            if (request.CharacterId.IsEmpty) {
                request.CharacterId = emitter.CharacterId;
            }
            request.Id = Requests.RequestIdGenerator.Alloc();
            request.State = VoxRequestPlaybackState.Queued;
            Requests.ActiveRequests.PushBack(request);

            emitter.RequestHandle = new VoxRequestHandle(request.Id);
            emitter.PlayingPriority = request.Priority;
            return emitter.RequestHandle;
        }

        #endregion // Speak

        #region Request Handle

        /// <summary>
        /// Stops playing the given request.
        /// </summary>
        static public void Stop(VoxRequestHandle handle) {
            KillVoxRequest(ref handle);
        }

        /// <summary>
        /// Stops playing the given request.
        /// </summary>
        static public void Stop(ref VoxRequestHandle handle) {
            KillVoxRequest(ref handle);
        }

        /// <summary>
        /// Returns if the given request is playing.
        /// </summary>
        static public bool IsPlaying(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return (req.State == VoxRequestPlaybackState.Loaded && req.StartPlayback) || req.State == VoxRequestPlaybackState.Playing;
        }

        /// <summary>
        /// Returns if the given request is loaded.
        /// </summary>
        static public bool IsReady(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return !req.LineCode.IsEmpty && req.State >= VoxRequestPlaybackState.Loaded;
        }

        /// <summary>
        /// Returns if the given request is loading.
        /// </summary>
        static public bool IsLoading(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return !req.LineCode.IsEmpty && req.State <= VoxRequestPlaybackState.Loading;
        }

        /// <summary>
        /// Starts playing the given request, once loaded.
        /// </summary>
        static public void Play(VoxRequestHandle id) {
            ref VoxRequest req = ref GetModifiableRequestData(id);
            if (!Unsafe.IsNullRef(ref req)) {
                req.StartPlayback = true;
            }
        }

        /// <summary>
        /// Retrieves the duration of the clip being played by the given request.
        /// </summary>
        static public float GetDuration(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            if (req.LoadedClip != null && req.LoadedClip.loadState == AudioDataLoadState.Loaded) {
                return req.LoadedClip.length;
            }

            return 0;
        }

        /// <summary>
        /// Retrieves the playback position of the clip being played by the given request.
        /// </summary>
        static public float GetPlaybackPosition(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            if (req.Emitter != null && req.LoadedClip != null) {
                return req.Emitter.Player.time;
            }

            return 0;
        }

        /// <summary>
        /// Retrieves the emitter for the given request.
        /// </summary>
        static public VoxEmitter GetEmitter(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.Emitter;
        }

        /// <summary>
        /// Retrieves the character id for the given request.
        /// </summary>
        static public StringHash32 GetCharacterId(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.CharacterId;
        }

        /// <summary>
        /// Retrieves the line code for the given request.
        /// </summary>
        static public StringHash32 GetLineCode(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.LineCode;
        }

        /// <summary>
        /// Retrieves the priority for the given request.
        /// </summary>
        static public VoxPriority GetPriority(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.Priority;
        }

        /// <summary>
        /// Retrieves the subtitle for the given request.
        /// </summary>
        static public string GetSubtitle(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.Subtitle.Data;
        }

        /// <summary>
        /// Retrieves the subtitle for the given request.
        /// </summary>
        static public SubtitleEntry GetSubtitleEntry(VoxRequestHandle id) {
            VoxRequest req = GetRequestData(id);
            return req.Subtitle;
        }

        #endregion // Request Handle
    }
}