using System;
using System.CodeDom.Compiler;
using System.IO;
using UnityEditor;

namespace FieldDay.Editor {
    static public class CodeGen {
        #region Symbols

        static public bool IsValidSymbol(string symbol) {
            if (string.IsNullOrEmpty(symbol)) {
                return false;
            }

            char c = symbol[0];
            if (!char.IsLetter(c) && c != '_') {
                return false;
            }

            for(int i = 1; i < symbol.Length; i++) {
                c = symbol[i];
                if (!char.IsLetterOrDigit(c) && c != '_') {
                    return false;
                }
            }

            return true;
        }

        static public string NameToSymbol(string name) {
            string safeName = ObjectNames.NicifyVariableName(name);
            safeName = safeName.Replace("-", "_").Replace(" ", ""); 
            if (!IsValidSymbol(safeName)) {
                throw new ArgumentException(string.Format("Cannot turn '{0}' into valid symbol name (attempted '{1}')", name, safeName), "name");
            }
            return safeName;
        }

        #endregion // Symbols

        #region Scopes

        static public bool OpenNamespace(this IndentedTextWriter writer, string ns) {
            return OpenScope(writer, "namespace", ns, null);
        }

        static public bool OpenStaticClass(this IndentedTextWriter writer, string className) {
            return OpenScope(writer, "static public class", className, null);
        }

        static public bool OpenScope(this IndentedTextWriter writer, string scopeType, string scopeName, string scopeSuffix) {
            if (!string.IsNullOrEmpty(scopeName)) {
                if (!IsValidSymbol(scopeName)) {
                    throw new ArgumentException("Invalid scope symbol", "scopeType");
                }

                writer.WriteLine();
                writer.Write(scopeType);
                writer.Write(" ");
                writer.Write(scopeName);
                if (!string.IsNullOrEmpty(scopeSuffix)) {
                    writer.Write(" ");
                    writer.Write(scopeSuffix);
                }
                writer.Write(" {");
                writer.WriteLine();
                writer.Indent++;
                return true;
            }

            return false;
        }

        static public void CloseScope(this IndentedTextWriter writer, string scope) {
            if (!string.IsNullOrEmpty(scope)) {
                writer.WriteLine();
                writer.Indent--;
                writer.Write("}");
            }
        }

        static public void CloseScope(this IndentedTextWriter writer) {
            writer.WriteLine();
            writer.Indent--;
            writer.Write("}");
        }

        static public void WriteComment(this IndentedTextWriter writer, string comment) {
            writer.WriteLine();
            writer.Write("// ");
            writer.Write(comment);
        }

        static public void WriteUsing(this IndentedTextWriter writer, string import) {
            writer.Write("using ");
            writer.Write(import);
            writer.Write(";");
            writer.WriteLine();
        }

        #endregion // Scopes

        /// <summary>
        /// Opens a code file for overwriting.
        /// </summary>
        static public IndentedTextWriter OpenCodeFile(Stream stream) {
            return new IndentedTextWriter(new StreamWriter(stream), "    ");
        }

        static public Stream OpenCodeStream(MonoScript script) {
            string path = AssetDatabase.GetAssetPath(script);
            if (!path.StartsWith("Assets/")) {
                throw new ArgumentException("MonoScript path is not within Assets", "script");
            }
            return File.Open(path, FileMode.Create);
        }
        
        /// <summary>
        /// Resolves the given script target, or prompts the user to create one.
        /// Returns if the target was valid.
        /// </summary>
        static public bool ResolveTarget(ref MonoScript script, string defaultName) {
            if (script == null) {
                string path = EditorUtility.SaveFilePanelInProject("Select Code File", Path.ChangeExtension(defaultName, "cs"), "cs", "Please choose where the code will be exported to");
                if (!string.IsNullOrEmpty(path)) {
                    try {
                        script = CreateMonoScript(path);
                        return true;
                    } catch(Exception e) {
                        EditorUtility.DisplayDialog("Error when saving", e.ToString(), "Whoops");
                    }
                }
            }

            return script;
        }

        static public MonoScript CreateMonoScript(string path) {
            if (!path.StartsWith("Assets/")) {
                throw new ArgumentException("MonoScript path is not within Assets", "path");
            }
            if (!path.EndsWith(".cs")) {
                throw new ArgumentException("MonoScript path does not end with .cs", "path");
            }

            string dir = Path.GetDirectoryName(path);
            Directory.CreateDirectory(dir);

            File.WriteAllText(path, "// TEMP FILE");

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }
    }
}