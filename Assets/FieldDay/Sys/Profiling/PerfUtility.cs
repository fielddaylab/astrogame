#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Diagnostics;
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

        static public float TargetFrameDurationMS() {
            return 1000f / TargetFramerate();
        }

    }
}