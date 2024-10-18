using System.CodeDom.Compiler;
using System.IO;
using ScriptableBake;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    [CreateAssetMenu(menuName = "Field Day/Code Gen/Scenes")]
    public class ScenesCodeGen : CodeGenBase, IBaked {
        public override void Generate(IndentedTextWriter writer, string ns) {
            writer.WriteUsing("BeauUtil");
            writer.WriteLine();

            writer.OpenNamespace(ns);
            {
                writer.OpenStaticClass("BuildScenes");
                {
                    int idx = 0;
                    var scenes = EditorBuildSettings.scenes;
                    for(int i = 0; i < scenes.Length; i++) {
                        var scene = scenes[i];

                        if (scene.enabled) {
                            string name = Path.GetFileNameWithoutExtension(scene.path);

                            string safeName = CodeGen.NameToSymbol(name);

                            writer.WriteComment("Scene " + i + ": " + name + " - " + scene.path);

                            writer.WriteLine();

                            writer.Write("public static readonly SceneReference ");
                            writer.Write(safeName);
                            writer.Write(" = new SceneReference(");
                            writer.Write(idx);
                            writer.Write(");");

                            idx++;
                        }
                    }
                }
                writer.CloseScope();
            }
            writer.CloseScope(ns);
        }

        int IBaked.Order {
            get { return 1000; }
        }

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            if (TargetFile != null) {
                WriteToFile(this);
                Baking.SetDirty(TargetFile);
                return true;
            }

            return false;
        }
    }
}