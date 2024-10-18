using FieldDay.Assets;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CustomEditor(typeof(AssetPack), true), CanEditMultipleObjects]
    public class AssetPackEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Reload From Directory")) {
                foreach(AssetPack pack in targets) {
                    AssetPack.ReadFromEditorDirectory(pack);
                }
            }
        }
    }
}