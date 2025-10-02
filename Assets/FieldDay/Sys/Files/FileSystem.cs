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

using BeauPools;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using FieldDay.Localization;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FieldDay.Files {
    public sealed class FileSystem {
        #region Types

        [Serializable]
        public struct Config {
            public int MaxInFlightRequests;
            public int MaxRetryCount;
            public float RetryDelay;
        }

        private struct InFlightFileRequest {
            public FileLoadRequest Request;
            public FileLoadPriority Priority;
            public UnityWebRequest UWR;
            public int RetryCount;
        }

        private struct RetryFileRequest {
            public long SendTimestamp;
            public FileLoadRequest Request;
            public FileLoadPriority Priority;
            public int RetryCount;
        }

        #endregion // Types

        #region Consts

        #endregion // Consts

        #region States

        private readonly RingBuffer<FileLoadRequest> m_HighPriorityRequests = new RingBuffer<FileLoadRequest>(8, RingBufferMode.Expand);
        private readonly RingBuffer<FileLoadRequest> m_LowPriorityRequests = new RingBuffer<FileLoadRequest>(8, RingBufferMode.Expand);

        private readonly RingBuffer<RetryFileRequest> m_RetryRequestQueue = new RingBuffer<RetryFileRequest>(8, RingBufferMode.Expand);
        private readonly RingBuffer<RetryFileRequest> m_RetryExhaustedQueue = new RingBuffer<RetryFileRequest>(8, RingBufferMode.Expand);

        private RingBuffer<InFlightFileRequest> m_InFlightRequests;
        private RingBuffer<UnityWebRequest> m_WebRequestsPendingDisposal;

        private int m_MaxRetries;
        private long m_RetryDelay;

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

        public void RequestFile(in FileLoadRequest request, FileLoadPriority priority) {
            Assert.NotNull(request.Callback, "Callback must be specified");
            Assert.True(!string.IsNullOrEmpty(request.Path), "Path must be specified");
            Assert.True(request.Callback != null, "Callback must be specified");

            switch (priority) {
                case FileLoadPriority.Urgent:
                    m_HighPriorityRequests.PushFront(request);
                    break;
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

        public void CancelRequestsInGroup(StringHash32 groupId) {
            Assert.False(groupId.IsEmpty, "Group must not be empty");
            m_HighPriorityRequests.RemoveWhere(FindRequestByGroup, groupId);
            m_LowPriorityRequests.RemoveWhere(FindRequestByGroup, groupId);
            m_RetryRequestQueue.RemoveWhere(FindRetryRequestByGroup, groupId);

            for (int i = m_InFlightRequests.Count; i-- > 0;) {
                ref InFlightFileRequest activeRequest = ref m_InFlightRequests[i];
                if (activeRequest.Request.Group == groupId) {
                    activeRequest.UWR.Abort();
                    activeRequest.UWR.Dispose();
                    m_InFlightRequests.FastRemoveAt(i);
                }
            }
        }

        public void CancelRequestsWithId(StringHash32 identifier) {
            Assert.False(identifier.IsEmpty, "Identifier must not be empty");
            m_HighPriorityRequests.RemoveWhere(FindRequestByName, identifier);
            m_LowPriorityRequests.RemoveWhere(FindRequestByName, identifier);
            m_RetryRequestQueue.RemoveWhere(FindRetryRequestByName, identifier);

            for (int i = m_InFlightRequests.Count; i-- > 0;) {
                ref InFlightFileRequest activeRequest = ref m_InFlightRequests[i];
                if (activeRequest.Request.Name == identifier) {
                    activeRequest.UWR.Abort();
                    activeRequest.UWR.Dispose();
                    m_InFlightRequests.FastRemoveAt(i);
                }
            }
        }

        public void CancelRequestsWithKey(uint key) {
            Assert.False(key == 0, "Identifier must not be empty");
            m_HighPriorityRequests.RemoveWhere(FindRequestByKey, key);
            m_LowPriorityRequests.RemoveWhere(FindRequestByKey, key);
            m_RetryRequestQueue.RemoveWhere(FindRetryRequestByKey, key);

            for (int i = m_InFlightRequests.Count; i-- > 0;) {
                ref InFlightFileRequest activeRequest = ref m_InFlightRequests[i];
                if (activeRequest.Request.PathKey == key) {
                    activeRequest.UWR.Abort();
                    activeRequest.UWR.Dispose();
                    m_InFlightRequests.FastRemoveAt(i);
                }
            }
        }

        static private Predicate<RetryFileRequest, StringHash32> FindRetryRequestByGroup = (a, b) => a.Request.Group == b;
        static private Predicate<RetryFileRequest, StringHash32> FindRetryRequestByName = (a, b) => a.Request.Name == b;
        static private Predicate<RetryFileRequest, uint> FindRetryRequestByKey = (a, b) => a.Request.PathKey == b;

        static private Predicate<FileLoadRequest, StringHash32> FindRequestByGroup = (a, b) => a.Group == b;
        static private Predicate<FileLoadRequest, StringHash32> FindRequestByName = (a, b) => a.Name == b;
        static private Predicate<FileLoadRequest, uint> FindRequestByKey = (a, b) => a.PathKey == b;


        #endregion // Requests

        #region Events

        internal void Initialize(FileSystem.Config config) {
            m_InFlightRequests = new RingBuffer<InFlightFileRequest>(config.MaxInFlightRequests, RingBufferMode.Fixed);
            m_WebRequestsPendingDisposal = new RingBuffer<UnityWebRequest>(config.MaxInFlightRequests + 2, RingBufferMode.Expand);

            s_StreamingPath = SanitizeDirectoryPath(Application.streamingAssetsPath);
            s_PersistentPath = SanitizeDirectoryPath(Application.persistentDataPath);
            s_TempCachePath = SanitizeDirectoryPath(Application.temporaryCachePath);

            m_MaxRetries = Math.Max(0, config.MaxRetryCount);
            m_RetryDelay = (long) (Stopwatch.Frequency * config.RetryDelay);
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

#if DEVELOPMENT
            if (GameLoop.IsPhase(GameLoopPhase.DebugUpdate)) {
                DebugRender();
            }
#endif // DEVELOPMENT
        }

        #endregion // Events

        #region Queue Processing

        private void ProcessInFlightRequests() {
            long ts = Frame.Timestamp();

            for (int i = m_InFlightRequests.Count - 1; i >= 0; i--) {
                ref InFlightFileRequest req = ref m_InFlightRequests[i];
                if (req.UWR.isDone) {
                    Log.Msg("[FileSystem] Request for '{0}' done", req.UWR.url);
                    if (!HandleError(ref req, ts)) {
                        CompleteRequest(ref req);
                    }
                    m_InFlightRequests.FastRemoveAt(i);
                }
            }
        }

        private bool HandleError(ref InFlightFileRequest request, long timestamp) {
            if (request.UWR.result == UnityWebRequest.Result.Success) {
                return false;
            }

            bool notFound;
            long responseCode = request.UWR.responseCode;
            if (responseCode >= 400 && responseCode < 500) {
                notFound = true;
                Log.Error("[FileSystem] Received response code {0}, not attempting any retry", responseCode);
            } else {
                notFound = false;
            }

            if (notFound) {
                return false;
            }

            bool hasRetriesRemaining = request.RetryCount < m_MaxRetries;
            bool canRetry = hasRetriesRemaining || (request.Request.Flags & FileLoadFlags.InfiniteRetries) != 0;

            RetryFileRequest retryRequest;
            retryRequest.Request = request.Request;
            retryRequest.Priority = request.Priority;
            retryRequest.RetryCount = request.RetryCount + 1;
            retryRequest.SendTimestamp = timestamp + m_RetryDelay;

            if (canRetry) {
                m_RetryRequestQueue.PushBack(retryRequest);
                Log.Warn("[FileSystem] Request failed, pushing to retry queue");
                return true;
            }

            if ((request.Request.Flags & FileLoadFlags.PushToExhaustedQueueOnFailure) != 0) {
                retryRequest.SendTimestamp = 0;
                m_RetryExhaustedQueue.PushBack(retryRequest);
                Log.Warn("[FileSystem] Request failed too many times, pushing to exhausted queue");
                return true;
            }

            return false;
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
            int retrySlots = Math.Min(requestSlotsRemaining - highPrioritySlots - lowPrioritySlots, m_RetryRequestQueue.Count);

            while(highPrioritySlots-- > 0) {
                KickRequest(m_HighPriorityRequests.PopFront(), FileLoadPriority.High, 0);
            }

            while(lowPrioritySlots-- > 0) {
                KickRequest(m_LowPriorityRequests.PopFront(), FileLoadPriority.Low, 0);
            }

            long now = Frame.Timestamp();
            while (retrySlots-- > 0) {
                RetryFileRequest retryRequest = m_RetryRequestQueue.PeekFront();
                if (now < retryRequest.SendTimestamp) {
                    break;
                }
                m_RetryRequestQueue.PopFront();
                KickRequest(retryRequest.Request, retryRequest.Priority, retryRequest.RetryCount + 1);
            }
        }

        private void KickRequest(in FileLoadRequest request, FileLoadPriority priority, int retryCount) {
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
                    if ((request.Flags & FileLoadFlags.Audio_Compressed) != 0) {
                        audio.compressed = true;
                    }
                    if ((request.Flags & FileLoadFlags.Audio_Streaming) != 0) {
                        audio.streamAudio = true;
                    }
                    uwr.downloadHandler = audio;
                    break;
                }
            }

            uwr.SendWebRequest();

            InFlightFileRequest inFlightRequest;
            inFlightRequest.Request = request;
            inFlightRequest.Priority = priority;
            inFlightRequest.UWR = uwr;
            inFlightRequest.RetryCount = retryCount;

            m_InFlightRequests.PushBack(inFlightRequest);

            Log.Msg("[FileSystem] Kicking request for '{0}'", resolvedPath);
        }

        private void KillWebRequestsPendingDisposal() {
            while(m_WebRequestsPendingDisposal.TryPopFront(out UnityWebRequest uwr)) {
                uwr.Abort();
                uwr.Dispose();
            }
        }

        #endregion // Queue Processing

        #region Path Resolution

        /// <summary>
        /// Resolves a path to a url for the given storage location.
        /// </summary>
        static public string ResolvePathToUrl(string path, FileLocation location) {
            s_PathBuilder.Clear();
            Assert.True(path.Length > 0, "Cannot provide empty path");
            bool firstCharIsSlash = path[0] == '/' || path[0] == '\\';
            if (firstCharIsSlash) {
                s_PathBuilder.Append(path, 1, path.Length - 1);
            } else {
                s_PathBuilder.Append(path);
            }

            SanitizePath(s_PathBuilder);
            Loc.Path(s_PathBuilder);
            MakeLocationSpecific(s_PathBuilder, location);

            if (!IsUrl(s_PathBuilder)) {
                MakeFileUrl(s_PathBuilder);
            }
            return s_PathBuilder.Flush();
        }

        /// <summary>
        /// Resolves a path to a url for the given storage location.
        /// </summary>
        static public void ResolvePathToUrl(StringBuilder path, FileLocation location) {
            s_PathBuilder.Clear();
            Assert.True(path.Length > 0, "Cannot provide empty path");
            s_PathBuilder.Append(path);
            path.Clear();

            bool firstCharIsSlash = s_PathBuilder[0] == '/' || s_PathBuilder[0] == '\\';
            if (firstCharIsSlash) {
                path.Append(s_PathBuilder, 1, s_PathBuilder.Length - 1);
            } else {
                path.Append(s_PathBuilder);
            }

            s_PathBuilder.Clear();

            SanitizePath(path);
            Loc.Path(path);
            MakeLocationSpecific(path, location);

            if (!IsUrl(path)) {
                MakeFileUrl(path);
            }
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

        /// <summary>
        /// Calculates a hash of the given path and file location.
        /// </summary>
        static public uint CalculatePathHash(string path, FileLocation location) {
            Assert.NotNull(path);
            uint pathHash = StringHash32.Fast(path).HashValue;
            pathHash = (pathHash & 0xFF000000) ^ (pathHash << 2) | (uint) location;
            return pathHash;
        }

        /// <summary>
        /// Calculates a hash of the given path and file location.
        /// </summary>
        static public uint CalculatePathHash(StringBuilder path, FileLocation location) {
            Assert.NotNull(path);
            uint pathHash = StringHash32.Fast(path, 0, path.Length).HashValue;
            pathHash = (pathHash & 0xFF000000) ^ (pathHash << 2) | (uint)location;
            return pathHash;
        }

        #endregion // Utilities

        #region Debug

        private enum DebuggingFlags {
            DisplayStats,
            DisplayQueues
        }

#if DEVELOPMENT

        private void DebugRender() {
            if (DebugFlags.IsFlagSet(DebuggingFlags.DisplayStats)) {
                using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                    int totalRequests = m_HighPriorityRequests.Count + m_LowPriorityRequests.Count + m_InFlightRequests.Count + m_RetryRequestQueue.Count;
                    psb.Builder.Append("Total File Requests: ").AppendNoAlloc(totalRequests)
                        .Append("\n   Running: ").AppendNoAlloc(m_InFlightRequests.Count)
                        .Append("\n   Queued (High Priority): ").AppendNoAlloc(m_HighPriorityRequests.Count)
                        .Append("\n   Queued (Low Priority): ").AppendNoAlloc(m_LowPriorityRequests.Count)
                        .Append("\n   Queued (Retry): ").AppendNoAlloc(m_RetryRequestQueue.Count)
                        .Append("\n   Retries Exhausted Queue: ").AppendNoAlloc(m_RetryExhaustedQueue.Count);

                    DebugDraw.AddLogText(psb, ColorBank.LimeGreen);
                }
            }
        }

        [EngineMenuFactory]
        static private DMInfo CreateFileDebugMenu() {
            DMInfo info = new DMInfo("Filesystem", 16);
            DebugFlags.Menu.AddFlagToggle(info, "Display Stats", DebuggingFlags.DisplayStats);
            DebugFlags.Menu.AddFlagToggle(info, "Display Queues", DebuggingFlags.DisplayQueues);

            return info;
        }

#endif // DEVELOPMENT

        #endregion // Debug

    }
}