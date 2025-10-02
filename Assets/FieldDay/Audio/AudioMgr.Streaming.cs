#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using System.IO;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Files;
using FieldDay.Filters;
using FieldDay.Localization;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Consts

        static private readonly StringHash32 FileGroup_Streams = "AudioMgr.Streams";
        static private readonly StringHash32 FileGroup_LocalizedStreams = "AudioMgr.LocalizedStreams";

        #endregion // Consts

        #region Streaming Clip

        private sealed unsafe class StreamedClip {
            public uint Id;

            public ushort RefCount;
            public ushort EventCount;
            public StreamedClipFlags Flags;
            public FileLocation Location;

            public string Path;
            public AudioClip Clip;
        }

        [Flags]
        private enum StreamedClipFlags : byte {
            Loading = 0x01,
            Loaded = 0x02,
            Error = 0x04,
            IsLocalizedPath = 0x08,
            EagerUnload = 0x10,

            LoadingStateMask = Loading | Loaded | Error
        }

        #endregion // Streaming Clip

        #region Clip Cache

        private StreamedClip GetStreamedClip(uint key) {
            int idx = m_ActiveStreamedClips.FindIndex(FindStreamedClipWithId, key);
            Assert.True(idx >= 0, "No streamed clip with the given id exists");
            return m_ActiveStreamedClips[idx];
        }

        private StreamedClip GetOrCreateStreamedClip(uint key, string path, FileLocation location) {
            int idx = m_ActiveStreamedClips.FindIndex(FindStreamedClipWithId, key);
            if (idx >= 0) {
                return m_ActiveStreamedClips[idx];
            }

            StreamedClip clip = m_StreamedClipPool.Alloc();
            clip.Id = key;
            clip.RefCount = 0;
            clip.Flags = 0;

            clip.Path = path;
            clip.Location = location;
            clip.Clip = null;

            if (Loc.IsLocalizedPath(path)) {
                clip.Flags |= StreamedClipFlags.IsLocalizedPath;
            }

            m_ActiveStreamedClips.PushBack(clip);
            return clip;
        }

        private void UnloadOneUnusedStreamedClip() {
            for (int i = m_ActiveStreamedClips.Count; i-- > 0; ) {
                StreamedClip clip = m_ActiveStreamedClips[i];
                if (clip.RefCount == 0) {
                    FreeStreamedClip(clip);
                    m_ActiveStreamedClips.FastRemoveAt(i);
                    return;
                } else if ((clip.Flags & StreamedClipFlags.EagerUnload) != 0) {
                    int activeInstances = clip.RefCount - clip.EventCount;
                    if (activeInstances == 0) {
                        UnloadStreamed(clip);
                        clip.Flags &= ~StreamedClipFlags.EagerUnload;
                    }
                }
            }
        }

        private void FreeStreamedClip(StreamedClip clip) {
            UnloadStreamed(clip);

            clip.Id = 0;
            clip.Path = null;
            clip.Location = default;
            clip.Flags = 0;
            clip.RefCount = 0;

            m_StreamedClipPool.Free(clip);
        }

        static private void UnloadStreamed(StreamedClip clip) {
            Assert.NotNull(clip);
            Assert.True(clip.Id != 0);

            if ((clip.Flags & StreamedClipFlags.Loading) != 0) {
                Game.Files.CancelRequestsWithKey(clip.Id);
            }

            if (clip.Clip != null) {
                Log.Msg("[AudioMgr] Unloading streamed clip '{0}'", clip.Path);
                AssetUtility.ManualUnload(clip.Clip);
                clip.Clip = null;
            }
            clip.Flags &= ~StreamedClipFlags.LoadingStateMask;
        }

        static private void LoadStreamed(StreamedClip clip, FileLoadPriority priority) {
            Assert.NotNull(clip);
            Assert.True(clip.Id != 0);

            if ((clip.Flags & StreamedClipFlags.LoadingStateMask) != 0) {
                return;
            }

            FileLoadRequest request;
            request.Path = clip.Path;
            request.PathKey = clip.Id;
            request.Location = clip.Location;
            request.Callback = OnStreamedClipLoaded;
            request.CallbackContext = clip;
            request.Flags = FileLoadFlags.Audio_Streaming;
            request.Mode = FileBufferMode.AudioClip;
            request.Name = default;
            request.Group = (clip.Flags & StreamedClipFlags.IsLocalizedPath) != 0 ? FileGroup_LocalizedStreams : FileGroup_Streams;

            clip.Flags = (clip.Flags & ~StreamedClipFlags.LoadingStateMask) | StreamedClipFlags.Loading;
            Game.Files.RequestFile(request, FileLoadPriority.High);
        }

        static private readonly Predicate<StreamedClip, uint> FindStreamedClipWithId = (a, b) => a.Id == b;

        static private void OnStreamedClipLoaded(FileLoadRequest request, FileLoadResult result, object context) {
            StreamedClip clip = (StreamedClip)context;
            if (request.PathKey != clip.Id || (clip.Flags & StreamedClipFlags.Loading) == 0) {
                return;
            }

            if (result.Succeeded()) {
                clip.Flags = (clip.Flags & ~StreamedClipFlags.LoadingStateMask) | StreamedClipFlags.Loaded;
                clip.Clip = result.ReadAudioClip();
#if DEVELOPMENT
                clip.Clip.name = Path.GetFileNameWithoutExtension(clip.Path);
#endif // DEVELOPMENT
                Log.Msg("[AudioMgr] Streamed clip '{0}' is ready", clip.Path);
            } else {
                clip.Flags = (clip.Flags & ~StreamedClipFlags.LoadingStateMask) | StreamedClipFlags.Error;
                clip.Clip = null;
                Log.Error("[AudioMgr] Streamed clip '{0}' was unable to be loaded...", clip.Path);
            }
        }

        #endregion // Clip Cache
    }
}