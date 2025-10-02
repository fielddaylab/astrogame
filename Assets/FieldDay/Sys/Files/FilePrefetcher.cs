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
    public sealed class FilePrefetcher {
        #region Types

        #endregion // Types
    }
}