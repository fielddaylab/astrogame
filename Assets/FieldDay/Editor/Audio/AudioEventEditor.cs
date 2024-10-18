using FieldDay.Assets;
using UnityEditor;
using UnityEngine;
using FieldDay.Audio;

namespace FieldDay.Editor {
    [CustomEditor(typeof(AudioEvent), true), CanEditMultipleObjects]
    public class AudioEventEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
        }
    }
}