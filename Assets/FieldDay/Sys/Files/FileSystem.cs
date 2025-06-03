#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
#define FILE_SYSTEM_WINDOWS
#elif UNITY_EDITOR
#define FILE_SYSTEM_DEFAULT
#elif UNITY_ANDROID || UNITY_WEBGL
#define FILE_SYSTEM_URL
#else
#define FILE_SYSTEM_DEFAULT
#endif // UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA

using System;
using System.IO;
using System.Text;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Localization;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay.Files {
    public sealed class FileSystem {
        #region Types

        [Serializable]
        public struct Config {
            public int MaxInFlightRequests;
        }

        private struct InFlightFileRequest {
            public FileLoadRequest Request;
            public FileLoadPriority Priority;
            public UnityWebRequest UWR;
        }

        #endregion // Types

        #region States

        private readonly RingBuffer<FileLoadRequest> m_HighPriorityRequests = new RingBuffer<FileLoadRequest>(8, RingBufferMode.Expand);
        private readonly RingBuffer<FileLoadRequest> m_LowPriorityRequests = new RingBuffer<FileLoadRequest>(8, RingBufferMode.Expand);

        private RingBuffer<InFlightFileRequest> m_InFlightRequests;
        private RingBuffer<UnityWebRequest> m_WebRequestsPendingDisposal;

        static private string s_StreamingPath;
        static private string s_PersistentPath;
        static private string s_TempCachePath;

        static private StringBuilder s_PathBuilder = new StringBuilder(270);

        #endregion // States

        #region Queries

        /// <summary>
        /// Returns if any high priority requests are loading or queued.
        /// </summary>
        public bool AnyHighPriorityRequestsLoading() {
            if (m_HighPriorityRequests.Count > 0) {
                return true;
            }

            foreach (var request in m_InFlightRequests) {
                if (request.Priority == FileLoadPriority.High) {
                    return true;
                }
            }

            return false;
        }

        #endregion // Queries

        #region Requests

        public void RequestFile(in FileLoadRequest request, FileLoadPriority priority = FileLoadPriority.High) {
            Assert.NotNull(request.Callback, "Callback must be specified");
            Assert.True(!string.IsNullOrEmpty(request.Path), "Path must be specified");
            switch (priority) {
                case FileLoadPriority.High:
                    m_HighPriorityRequests.PushBack(request);
                    break;
                case FileLoadPriority.Low:
                    m_LowPriorityRequests.PushBack(request);
                    break;
                default:
                    Assert.Fail("Unknown file priority");
                    break;
            }
        }

        #endregion // Requests

        #region Events

        internal void Initialize(FileSystem.Config config) {
            m_InFlightRequests = new RingBuffer<InFlightFileRequest>(config.MaxInFlightRequests, RingBufferMode.Fixed);
            m_WebRequestsPendingDisposal = new RingBuffer<UnityWebRequest>(config.MaxInFlightRequests + 2, RingBufferMode.Expand);

            s_StreamingPath = SanitizeDirectoryPath(Application.streamingAssetsPath);
            s_PersistentPath = SanitizeDirectoryPath(Application.persistentDataPath);
            s_TempCachePath = SanitizeDirectoryPath(Application.temporaryCachePath);
        }

        internal void Shutdown() {
            foreach (var activeRequest in m_InFlightRequests) {
                activeRequest.UWR.Abort();
                activeRequest.UWR.Dispose();
            }

            m_InFlightRequests.Clear();

            m_HighPriorityRequests.Clear();
            m_LowPriorityRequests.Clear();
        }

        internal void Tick() {
            KillWebRequestsPendingDisposal();
            ProcessInFlightRequests();
            SendNewRequests();
        }

        #endregion // Events

        #region Queue Processing

        private void ProcessInFlightRequests() {
            for (int i = m_InFlightRequests.Count - 1; i >= 0; i--) {
                ref InFlightFileRequest req = ref m_InFlightRequests[i];
                if (req.UWR.isDone) {
                    CompleteRequest(ref req);
                    m_InFlightRequests.FastRemoveAt(i);
                }
            }
        }

        private void CompleteRequest(ref InFlightFileRequest request) {
#if DEVELOPMENT
            try {
                request.Request.Callback(request.Request, new FileLoadResult(request.UWR), request.Request.CallbackContext);
            } finally {
                m_WebRequestsPendingDisposal.PushBack(request.UWR);
            }
#else
            request.Request.Callback(request.Request, new FileLoadResult(request.UWR), request.Request.CallbackContext);
            m_WebRequestsPendingDisposal.PushBack(request.UWR);
#endif // DEVELOPMENT

        }

        private void SendNewRequests() {
            int requestSlotsRemaining = m_InFlightRequests.Capacity - m_InFlightRequests.Count;
            int highPrioritySlots = Math.Min(requestSlotsRemaining, m_HighPriorityRequests.Count);
            int lowPrioritySlots = Math.Min(requestSlotsRemaining - highPrioritySlots, m_LowPriorityRequests.Count);

            while(highPrioritySlots-- > 0) {
                KickRequest(m_HighPriorityRequests.PopFront(), FileLoadPriority.High);
            }

            while(lowPrioritySlots-- > 0) {
                KickRequest(m_LowPriorityRequests.PopFront(), FileLoadPriority.Low);
            }
        }

        private void KickRequest(in FileLoadRequest request, FileLoadPriority priority) {
            string resolvedPath = ResolvePathToUrl(request.Path, request.Location);

            UnityWebRequest uwr = UnityWebRequest.Get(new Uri(resolvedPath));
            switch (request.Mode) {
                case FileBufferMode.Buffer: {
                    DownloadHandlerBuffer buffer = new DownloadHandlerBuffer();
                    uwr.downloadHandler = buffer;
                    break;
                }
                case FileBufferMode.Texture: {
                    DownloadHandlerTexture texture = new DownloadHandlerTexture((request.Flags & FileLoadFlags.Texture_MarkNonReadable) == 0);
                    uwr.downloadHandler = texture;
                    break;
                }
                case FileBufferMode.AudioClip: {
                    DownloadHandlerAudioClip audio = new DownloadHandlerAudioClip(resolvedPath, AudioType.UNKNOWN);
                    audio.compressed = (request.Flags & FileLoadFlags.Audio_Compressed) != 0;
                    audio.streamAudio = (request.Flags & FileLoadFlags.Audio_Streaming) != 0;
                    uwr.downloadHandler = audio;
                    break;
                }
            }

            uwr.SendWebRequest();

            InFlightFileRequest inFlightRequest;
            inFlightRequest.Request = request;
            inFlightRequest.Priority = priority;
            inFlightRequest.UWR = uwr;

            m_InFlightRequests.PushBack(inFlightRequest);
        }

        private void KillWebRequestsPendingDisposal() {
            while(m_WebRequestsPendingDisposal.TryPopFront(out UnityWebRequest uwr)) {
                uwr.Abort();
                uwr.Dispose();
            }
        }

        #endregion // Queue Processing

        #region Path Resolution

        static private string ResolvePathToUrl(string path, FileLocation location) {
            s_PathBuilder.Clear();
            Assert.True(path.Length > 0, "Cannot provide empty path");
            bool firstCharIsSlash = path[0] == '/' || path[0] == '\\';
            if (firstCharIsSlash) {
                s_PathBuilder.Append(path, 1, path.Length - 1);
            } else {
                s_PathBuilder.Append(path);
            }

            SanitizePath(path);
            Loc.Path(s_PathBuilder);
            MakeLocationSpecific(s_PathBuilder, location);

            if (!IsUrl(path)) {
                MakeFileUrl(s_PathBuilder);
            }
            return s_PathBuilder.Flush();
        }

        static private void MakeLocationSpecific(StringBuilder path, FileLocation location) {
            switch (location) {
                case FileLocation.Persistent:
                    path.Insert(0, s_PersistentPath);
                    break;
                case FileLocation.Streaming:
                    path.Insert(0, s_StreamingPath);
                    break;
                case FileLocation.TempCachePath:
                    path.Insert(0, s_TempCachePath);
                    break;
            }
        }

        static private void MakeFileUrl(StringBuilder path) {
#if FILE_SYSTEM_WINDOWS
            path.Insert(0, "file:///");
#elif FILE_SYSTEM_DEFAULT
            path.Insert(0, "file://");
#endif // FILE_SYSTEM_DEFAULT
        }

        #endregion // Path Resolution

        #region Utilities

        /// <summary>
        /// Returns if the given path is a url.
        /// </summary>
        static public bool IsUrl(string path) {
            return path != null && path.Contains("://");
        }

        /// <summary>
        /// Returns if the given path is a url.
        /// </summary>
        static public bool IsUrl(StringBuilder path) {
            return path != null && path.IndexOf("://") >= 0;
        }

        /// <summary>
        /// Sanitizes all slashes to be forward slashes.
        /// </summary>
        static public string SanitizePath(string path) {
            Assert.NotNull(path);
            return path.Replace('\\', '/');
        }

        /// <summary>
        /// Sanitizes all slashes to be forward slashes,
        /// and ensures a forward slash at the end.
        /// </summary>
        static public string SanitizeDirectoryPath(string path) {
            Assert.NotNull(path);
            path = path.Replace('\\', '/');
            if (path.Length > 0 && path[path.Length - 1] != '/') {
                path += '/';
            }
            return path;
        }

        /// <summary>
        /// Sanitizes all slashes to be forward slashes.
        /// </summary>
        static public StringBuilder SanitizePath(StringBuilder path) {
            Assert.NotNull(path);
            path.Replace('\\', '/');
            return path;
        }

        /// <summary>
        /// Sanitizes all slashes to be forward slashes,
        /// and ensures a forward slash at the end.
        /// </summary>
        static public StringBuilder SanitizeDirectoryPath(StringBuilder path) {
            Assert.NotNull(path);
            path.Replace('\\', '/');
            if (path.Length > 0 && path[path.Length - 1] != '/') {
                path.Append('/');
            }
            return path;
        }

        /// <summary>
        /// Sanitizes all slashes to be forward slashes.
        /// </summary>
        static public StringBuilder SanitizePath(StringBuilder path, int startIndex, int count) {
            Assert.NotNull(path);
            path.Replace('\\', '/', startIndex, count);
            return path;
        }

        #endregion // Utilities
    }
}