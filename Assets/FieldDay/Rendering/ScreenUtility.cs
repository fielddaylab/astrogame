#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FieldDay.Rendering {
    static public class ScreenUtility {
#if UNITY_WEBGL && !UNITY_EDITOR

        [DllImport("__Internal")]
        static private extern void NativeFullscreen_SetFullscreen(bool fullscreen);

#endif // UNITY_WEBGL && !UNITY_EDITOR

        /// <summary>
        /// Sets the fullscreen mode.
        /// </summary>
        static public void SetFullscreen(bool fullscreen) {
#if USE_JSLIB
            NativeFullscreen_SetFullscreen(fullscreen);
#elif UNITY_EDITOR
            // TODO: fullscreen within editor?
            Screen.fullScreen = fullscreen;
#else
            Screen.fullScreen = fullscreen;
#endif // UNITY_WEBGL && !UNITY_EDITOR
        }

        /// <summary>
        /// Returns the fullscreen mode.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool GetFullscreen() {
            return Screen.fullScreen;
        }

        /// <summary>
        /// Returns the current resolution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public Resolution GetResolution() {
#if UNITY_EDITOR || UNITY_WEBGL
            return new Resolution() {
                width = Screen.width,
                height = Screen.height,
#if UNITY_2022_2_OR_NEWER
                refreshRateRatio = new RefreshRate() { numerator = 60, denominator = 1 }
#else
                refreshRate = 60
#endif // UNITY_2022_2_OR_NEWER
            };
#else
            return Screen.currentResolution;
#endif // UNITY_EDITOR || UNITY_WEBGL
            }
    }
}