#if !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT
#define STRIP_HASH_STRINGS
#endif // !PRESERVE_DEBUG_SYMBOLS && !DEVELOPMENT

using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Data {
    /// <summary>
    /// Interface for class that has data
    /// that can be stripped out for builds.
    /// </summary>
    public interface IEditorOnlyData {
#if UNITY_EDITOR
        void ClearEditorData(bool isDevelopmentBuild);
#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Utility methods for stripping editor data.
    /// </summary>
    static public class EditorOnlyData {
#if UNITY_EDITOR

        /// <summary>
        /// Strips the given hash of its source string.
        /// </summary>
        static public void Strip(ref SerializedHash32 hash) {
#if STRIP_HASH_STRINGS
            hash = new SerializedHash32(hash.Hash());
#endif // STRIP_HASH_STRINGS
        }

#endif // UNITY_EDITOR
    }
}
