#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System;
using BeauUtil;
using FieldDay.Files;
using FieldDay.Filters;
using UnityEngine;

namespace FieldDay.Audio {
    public sealed partial class AudioMgr {
        #region Streaming Entry

        private sealed unsafe class StreamedClip {
            public ushort RefCount;
            public StreamedClipState State;
            public FileLocation Location;

            public string Path;
            public AudioClip Clip;
        }

        private enum StreamedClipState : byte {
            Unloaded,
            Loading,
            Loaded,
            Error
        }

        #endregion // Streaming Entry
    }
}