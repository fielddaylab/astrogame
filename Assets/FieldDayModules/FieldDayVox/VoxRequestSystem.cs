using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Audio;
using FieldDay.Systems;

namespace FieldDay.Vox {
    [SysUpdate(GameLoopPhaseMask.PreUpdate | GameLoopPhaseMask.UnscaledUpdate | GameLoopPhaseMask.UnscaledLateUpdate, 1001)]
    internal class VoxRequestSystem : ISystem {
        public bool HasWork() {
            return VoxUtility.Requests != null;
        }

        public void ProcessWork(float deltaTime) {
            var requestBuffer = VoxUtility.Requests.ActiveRequests;
            for(int i = requestBuffer.Count - 1; i >= 0; i--) {
                ref VoxRequest req = ref requestBuffer[i];
                switch (req.State) {
                    case VoxRequestPlaybackState.Queued: {
                        SetupInternalData(ref req);

                        if (!CheckLoaded(ref req)) {
                            VoxUtility.PushImmediateLoad(req.LineCode);
                            req.State = VoxRequestPlaybackState.Loading;
                        }
                        break;
                    }

                    case VoxRequestPlaybackState.Loading: {
                        CheckLoaded(ref req);
                        break;
                    }

                    case VoxRequestPlaybackState.Loaded: {
                        if (req.StartPlayback) {
                            BeginPlaying(ref req);
                        }
                        break;
                    }

                    case VoxRequestPlaybackState.Playing: {
                        if (!Sfx.IsActive(req.Emitter.AudioHandle)) {
                            Log.Debug("[VoxRequestSystem] Playback from VoxEmitter '{0}' completed", GetDebugName(req.Emitter));
                            req.State = VoxRequestPlaybackState.Complete;
                        }
                        break;
                    }
                }

                if (req.State == VoxRequestPlaybackState.Complete) {
                    VoxUtility.KillVoxRequestAtIndex(ref req, i);
                }
            }
        }

        static private void SetupInternalData(ref VoxRequest req) {
            req.Subtitle = SubtitleUtility.FindSubtitle(req.LineCode, req.Subtitle);
        }

        static private bool CheckLoaded(ref VoxRequest req) {
            if (VoxUtility.TryGetData(req.LineCode, out VoxFileData file)) {
                if (file.Clip == null) {
                    Log.Warn("[VoxRequestSystem] No clip loaded for line '{0}' - skipping voice playback from VoxEmitter '{1}'", req.LineCode, GetDebugName(req.Emitter));
                    req.State = VoxRequestPlaybackState.Complete;
                } else {
                    req.LoadedClip = file.Clip;

                    if (req.StartPlayback) {
                        BeginPlaying(ref req);
                    } else {
                        req.State = VoxRequestPlaybackState.Loaded;
                    }
                }

                return true;
            }

            return false;
        }

        static private void BeginPlaying(ref VoxRequest req) {
            req.State = VoxRequestPlaybackState.Playing;

            StringHash32 eventId = req.EventOverride.IsEmpty ? req.Emitter.DefaultPlaybackEvent : req.EventOverride;
            req.Emitter.AudioHandle = Sfx.PlayFrom(eventId, req.LoadedClip, req.Emitter.Player);
            Log.Debug("[VoxRequestSystem] Playing clip '{0}' from VoxEmitter '{1}'", req.LoadedClip.name, GetDebugName(req.Emitter));

            VoxRequest cachedReq = req;
            VoxRequestHandle handle = new VoxRequestHandle(req.Id);

            if (VoxUtility.Requests.OnAnyVoiceSpeak != null) {
                VoxUtility.Requests.OnAnyVoiceSpeak(handle, cachedReq.Emitter, cachedReq.LineCode);
            }

            cachedReq.Emitter.OnStartSpeaking.Invoke(handle, cachedReq.LineCode);

            if (cachedReq.OnSpeak != null) {
                cachedReq.OnSpeak(handle, cachedReq.Emitter, cachedReq.LineCode);
            }

            SubtitleUtility.RequestDisplay(new SubtitleDisplayData() {
                CharacterId = cachedReq.Emitter.CharacterId,
                Subtitle = cachedReq.Subtitle,
                Priority = cachedReq.Priority,
                VoxHandle = handle
            });
        }

        static private string GetDebugName(VoxEmitter emitter) {
            if (emitter.CharacterId.IsEmpty) {
                return emitter.gameObject.name;
            } else {
                return emitter.CharacterId.ToDebugString();
            }
        }

        public void Initialize() {
        }

        public void Shutdown() {
        }
    }
}