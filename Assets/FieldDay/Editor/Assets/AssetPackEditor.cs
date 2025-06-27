using FieldDay.Assets;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CustomEditor(typeof(AssetPack), true), CanEditMultipleObjects]
    public class AssetPackEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            using (new EditorGUI.DisabledScope(true)) {
                base.OnInspectorGUI();
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("Asset packs cannot be manually modified.\nEnsure the assets you want in the package are located in the directory or its subdirectories.", MessageType.Info);

            if (GUILayout.Button("Reload From Directory")) {
                foreach(AssetPack pack in targets) {
                    AssetPack.ReadFromEditorDirectory(pack);
                }
            }
        }
    }
}