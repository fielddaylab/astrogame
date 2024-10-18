#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Audio {
    static public class AudioUtility {
        /// <summary>
        /// Attempts to wake up native audio playback.
        /// </summary>
        static public void WakeUpNativeAudio() {
#if USE_JSLIB
            if (NativeWebAudio_WakeUp()) {
                Log.Msg("[AudioUtility] Web audio was suspended");
            }
#endif // USE_JSLIB
        }

#if USE_JSLIB
        [DllImport("__Internal")]
        static private extern bool NativeWebAudio_WakeUp();
#endif // USE_JSLIB
    }
}