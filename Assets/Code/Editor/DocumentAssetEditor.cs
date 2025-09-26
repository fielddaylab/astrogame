using Astro;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StreamingDocumentVisual))]
public class StreamingDocumentVisualPropertyDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(position, label, property);

        Rect labelPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(labelPosition, true, label);
        
        Rect propertyPosition = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propertyPosition, property.FindPropertyRelative("VisualAssetPath"));

        Rect propertyPosition2 = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight * 2, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.PropertyField(propertyPosition2, property.FindPropertyRelative("LowResAssetPath"));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUIUtility.singleLineHeight * 3;
    }
}

[CustomEditor(typeof(DocumentAsset))]
public class DocumentAssetEditor : Editor {
    private bool m_ShowTextRegions = false;
    private bool m_ShowVisualRegions = false;

    public override void OnInspectorGUI() {
        serializedObject.Update();
        DocumentAsset asset = target as DocumentAsset;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Category")); 
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Prefab"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Font")); 
        EditorGUILayout.PropertyField(serializedObject.FindProperty("TriggersPrompter")); 
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UseCutoutMaterial")); 
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();

        if (asset.Prefab != null) {
            m_ShowTextRegions = asset.Prefab.TextRegions.Length > 0 && EditorGUILayout.Foldout(m_ShowTextRegions, "Document Text");

            if ( m_ShowTextRegions ) {
                for (int i = 0; i < asset.Prefab.TextRegions.Length; i++) {
                    EditorGUILayout.LabelField(asset.Prefab.TextRegions[i].Text.gameObject.name);
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("TextFields").GetArrayElementAtIndex(i));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("LowResTextFields").GetArrayElementAtIndex(i));
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Document Positions", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("DefaultPinnedPos")); 
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ZoomOffsetOverride"));
        serializedObject.ApplyModifiedProperties();

        if (asset.Prefab == null) return;
        if (asset.Prefab.RenderComponents.Length <= 0) return;

        EditorGUILayout.Space();
        m_ShowVisualRegions = EditorGUILayout.Foldout(m_ShowVisualRegions, "Document Visuals");

        if (m_ShowVisualRegions) {
            EditorGUI.indentLevel += 1;
            for (int i = 0; i < asset.Prefab.RenderComponents.Length; i++) {
                EditorGUILayout.LabelField(asset.Prefab.RenderComponents[i].name + " (" + asset.Prefab.RenderComponents[i].GetType().ToString() + ")", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("StreamingVisuals").GetArrayElementAtIndex(i), true);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.Space();
            }
        }
    }
}