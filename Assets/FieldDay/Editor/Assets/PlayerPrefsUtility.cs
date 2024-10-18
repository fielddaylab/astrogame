using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    static public class PlayerPrefsUtility {
        [MenuItem("Field Day/PlayerPrefs/Clear")]
        static public void ClearPlayerPrefs() {
            if (EditorUtility.DisplayDialog("Clear Player Prefs?", "This cannot be undone", "Delete away!", "Wait no")) {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
            }
        }
    }
}