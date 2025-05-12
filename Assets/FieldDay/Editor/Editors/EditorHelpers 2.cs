using System.IO;
using UnityEditor;
using UnityEngine;

namespace FieldDay.Editor {
    static public class EditorHelpers {
        public struct ResourceSaveForm {
            public string LastSaveLocationKey;
            public string Header;
            public string FileExtension;
            public string Message;
        }

        static public bool SaveResourceAs<T>(T resource, string name, in ResourceSaveForm form) where T : UnityEngine.Object {
            string lastDirectory = !string.IsNullOrEmpty(form.LastSaveLocationKey) ? EditorPrefs.GetString(form.LastSaveLocationKey, "Assets/") : "Assets/";

            string path = EditorUtility.SaveFilePanelInProject(form.Header ?? string.Format("Save {0}", typeof(T).Name), name, form.FileExtension, form.Message, lastDirectory);
            if (!string.IsNullOrEmpty(path)) {
                T clone = GameObject.Instantiate(resource);
                clone.name = Path.GetFileNameWithoutExtension(path);

                AssetDatabase.CreateAsset(clone, path);

                if (!string.IsNullOrEmpty(form.LastSaveLocationKey)) {
                    lastDirectory = Path.GetDirectoryName(path);
                    EditorPrefs.SetString(form.LastSaveLocationKey, lastDirectory);
                }
                return true;
            }

            return false;
        }

        static public void DestroyResource<T>(ref T obj) where T : UnityEngine.Object {
            if (obj != null) {
                GameObject.DestroyImmediate(obj);
                obj = null;
            }
        }
    }
}