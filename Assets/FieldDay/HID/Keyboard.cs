#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_JSLIB
#endif // UNITY_WEBGL && !UNITY_EDITOR

using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.HID {
    static public class KeyboardUtility {
        /// <summary>
        /// Event dispatched when a key is pressed.
        /// </summary>
        static public readonly CastableEvent<KeyCode> OnKeyPressed = new CastableEvent<KeyCode>(4);

        /// <summary>
        /// Event dispatched when a key is released.
        /// </summary>
        static public readonly CastableEvent<KeyCode> OnKeyReleased = new CastableEvent<KeyCode>(4);
    }
}