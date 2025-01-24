#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.HID {
    static public class CursorUtility {
        /// <summary>
        /// Returns if the main cursor is within the game window.
        /// </summary>
        static public bool IsCursorWithinGameWindow() {
            Vector2 mousePos = Input.mousePosition;
            return mousePos.x >= 0 && mousePos.x < Screen.width
                && mousePos.y >= 0 && mousePos.y < Screen.height;
        }

        /// <summary>
        /// Hides the cursor.
        /// </summary>
        static public void HideCursor() {
            Cursor.visible = false;

#if USE_JSLIB
            NativeWebCursor_Hide();
#endif // USE_JSLIB
        }

        /// <summary>
        /// Returns if the cursor is currently showing.
        /// </summary>
        static public bool CursorIsShowing() {
            return Cursor.visible;
        }

        /// <summary>
        /// Shows the cursor.
        /// </summary>
        static public void ShowCursor() {
            Cursor.visible = true;

#if USE_JSLIB
            NativeWebCursor_Show();
#endif // USE_JSLIB
        }

        /// <summary>
        /// Platform-specific initialization.
        /// </summary>
        static internal void PlatformInit() {
#if USE_JSLIB
            NativeWebCursor_AutoFindCanvas();
            Cursor.visible = NativeWebCursor_IsVisible();
#endif // USE_JSLIB
        }

#if USE_JSLIB

        [DllImport("__Internal")]
        static private extern void NativeWebCursor_AutoFindCanvas();

        [DllImport("__Internal")]
        static private extern void NativeWebCursor_SetCanvasId(string canvasId);

        [DllImport("__Internal")]
        static private extern bool NativeWebCursor_IsVisible();

        [DllImport("__Internal")]
        static private extern void NativeWebCursor_Show();

        [DllImport("__Internal")]
        static private extern void NativeWebCursor_ShowType(string type);

        [DllImport("__Internal")]
        static private extern void NativeWebCursor_Hide();

#endif // USE_JSLIB
    }
}