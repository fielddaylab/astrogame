using System;
using System.CodeDom.Compiler;
using System.Drawing;
using ScriptableBake;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    public abstract class CodeGenBase : ScriptableObject {
        public MonoScript TargetFile;
        public string Namespace;

        public abstract void Generate(IndentedTextWriter writer, string ns);

        static public void WriteToFile(CodeGenBase gen) {
            if (gen.TargetFile != null) {
                using (var stream = CodeGen.OpenCodeStream(gen.TargetFile)) {
                    using (var writer = CodeGen.OpenCodeFile(stream)) {
                        gen.Generate(writer, gen.Namespace);
                        writer.InnerWriter.Flush();
                        stream.Flush();
                    }
                }
                EditorUtility.SetDirty(gen.TargetFile);
            }
        }

        [CustomEditor(typeof(CodeGenBase), true)]
        public class InspectorBase : UnityEditor.Editor {
            protected SerializedProperty m_TargetFileProp;
            protected SerializedProperty m_NamespaceProp;

            protected virtual void OnEnable() {
                m_TargetFileProp = serializedObject.FindProperty("TargetFile");
                m_NamespaceProp = serializedObject.FindProperty("Namespace");
            }

            public override void OnInspectorGUI() {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUILayout.LabelField("Targets", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(m_TargetFileProp);
                EditorGUILayout.PropertyField(m_NamespaceProp);

                bool isValid = true;

                if (!m_NamespaceProp.hasMultipleDifferentValues) {
                    string ns = m_NamespaceProp.stringValue;
                    if (!string.IsNullOrEmpty(ns) && !CodeGen.IsValidSymbol(ns)) {
                        EditorGUILayout.HelpBox("Invalid namespace", MessageType.Error);
                        isValid = false;
                    }
                } else {
                    isValid = false;
                }

                if (!RenderAdditionalProperties()) {
                    isValid = false;
                }

                using(new EditorGUI.DisabledScope(!isValid)) {
                    if (GUILayout.Button("Generate")) {
                        foreach(CodeGenBase gen in targets) {
                            Undo.RecordObject(gen, "Generating code file...");
                            if (CodeGen.ResolveTarget(ref gen.TargetFile, gen.name)) {
                                EditorUtility.SetDirty(gen);
                                try {
                                    serializedObject.ApplyModifiedProperties();
                                    using (var stream = CodeGen.OpenCodeStream(gen.TargetFile)) {
                                        using (var writer = CodeGen.OpenCodeFile(stream)) {
                                            gen.Generate(writer, m_NamespaceProp.stringValue);
                                            writer.InnerWriter.Flush();
                                            stream.Flush();
                                        }
                                    }
                                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(gen.TargetFile), ImportAssetOptions.ForceUpdate);
                                } catch(Exception e) {
                                    EditorUtility.DisplayDialog("Code Generation Error", e.ToString(), "Whoops");
                                }
                            }
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }


            public virtual bool RenderAdditionalProperties() {
                return true;
            }
        }
    }
}