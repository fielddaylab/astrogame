#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using UnityEngine;

namespace FieldDay.Perf {
    static public class PerfUtility {
        static public int TargetFramerate() {
            int framerate = Application.targetFrameRate;
            if (framerate <= 0) {
                return 60;
            }
            return framerate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float TargetFrameDurationMS() {
            return 1000f / TargetFramerate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsSecureContext() {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebPerf_IsCrossOriginIsolated();
#elif UNITY_EDITOR
            return true;
#else
            return Application.sandboxType == ApplicationSandboxType.Sandboxed;
#endif // UNITY_WEBGL && !UNITY_EDITOR
        }

#if UNITY_WEBGL
        [DllImport("__Internal")]
        static private extern bool WebPerf_IsCrossOriginIsolated();
#endif // UNITY_WEBGL
    }
}