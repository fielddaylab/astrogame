using FieldDay.Assets;
using UnityEditor;
using UnityEngine;
using FieldDay.Audio;

namespace FieldDay.Editor {
    [CustomEditor(typeof(AudioEvent), true), CanEditMultipleObjects]
    public class AudioEventEditor : UnityEditor.Editor {

        private SerializedProperty m_SamplesProperty;
        private SerializedProperty m_StreamProperty;
        private SerializedProperty m_PreloadSamplesProperty;
        
        private SerializedProperty m_VolumeProperty;
        private SerializedProperty m_VolumeMultiplierProperty;
        private SerializedProperty m_PitchProperty;
        private SerializedProperty m_PanProperty;
        private SerializedProperty m_DelayProperty;

        private SerializedProperty m_LoopProperty;
        private SerializedProperty m_RandomizeStartTimeProperty;
        private SerializedProperty m_RandomizePanSignProperty;

        private SerializedProperty m_BusProperty;
        private SerializedProperty m_PriorityProperty;
        private SerializedProperty m_EmitterConfigurationProperty;
        private SerializedProperty m_TagProperty;

        private void OnEnable() {
            m_SamplesProperty = serializedObject.FindProperty("Samples");
            m_StreamProperty = serializedObject.FindProperty("Stream");
            m_PreloadSamplesProperty = serializedObject.FindProperty("PreloadSamples");

            m_VolumeProperty = serializedObject.FindProperty("Volume");
            m_VolumeMultiplierProperty = serializedObject.FindProperty("VolumeMultiplier");
            m_PitchProperty = serializedObject.FindProperty("Pitch");
            m_PanProperty = serializedObject.FindProperty("Pan");
            m_DelayProperty = serializedObject.FindProperty("Delay");

            m_LoopProperty = serializedObject.FindProperty("Loop");
            m_RandomizeStartTimeProperty = serializedObject.FindProperty("RandomizeStartTime");
            m_RandomizePanSignProperty = serializedObject.FindProperty("RandomizePanSign");

            m_BusProperty = serializedObject.FindProperty("Bus");
            m_PriorityProperty = serializedObject.FindProperty("Priority");
            m_EmitterConfigurationProperty = serializedObject.FindProperty("EmitterConfiguration");
            m_TagProperty = serializedObject.FindProperty("Tag");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_StreamProperty);
            if (m_StreamProperty.hasMultipleDifferentValues || string.IsNullOrEmpty(m_StreamProperty.stringValue)) {
                EditorGUILayout.PropertyField(m_SamplesProperty);
            }
            EditorGUILayout.PropertyField(m_PreloadSamplesProperty);

            EditorGUILayout.PropertyField(m_VolumeProperty);
            EditorGUILayout.PropertyField(m_VolumeMultiplierProperty);
            EditorGUILayout.PropertyField(m_PitchProperty);
            EditorGUILayout.PropertyField(m_PanProperty);
            EditorGUILayout.PropertyField(m_DelayProperty);

            EditorGUILayout.PropertyField(m_LoopProperty);
            if (!m_LoopProperty.hasMultipleDifferentValues && m_LoopProperty.boolValue) {
                EditorGUILayout.PropertyField(m_RandomizeStartTimeProperty);
            }
            EditorGUILayout.PropertyField(m_RandomizePanSignProperty);

            EditorGUILayout.PropertyField(m_BusProperty);
            EditorGUILayout.PropertyField(m_PriorityProperty);
            EditorGUILayout.PropertyField(m_EmitterConfigurationProperty);
            EditorGUILayout.PropertyField(m_TagProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}