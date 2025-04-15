using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace FieldDay.Scenes {
    /// <summary>
    /// Loads another scene upon scene load.
    /// </summary>
    public sealed class ImportScene : MonoBehaviour {
        [Tooltip("The scene to load")]
        public SceneReference Scene;

        [Tooltip("Additional import flags")]
        [AutoEnum] public SceneImportFlags Flags = 0;

        [Tooltip("If checked, the provided scene will be merged into the active scene.")]
        public bool Merge = true;

        [Tooltip("If checked, this GameObject will be destroyed.")]
        public bool DestroyGameObject = true;

        [Tooltip("If provided, will transform the loaded scene by the given transform.")]
        public Transform Transform;

        [Tooltip("If set, this will unload any other loaded scenes with this tag.")]
        public SerializedHash32 Tag;

        /// <summary>
        /// Returns the import settings for the given scene.
        /// </summary>
        public SceneImportSettings GetImportSettings() {
            return new SceneImportSettings(Scene, Flags, Transform, Tag);
        }

        #region Utilities

        static private readonly List<GameObject> s_RootWorkList = new List<GameObject>(256);

        /// <summary>
        /// Transforms the given scene's roots by the given matrix.
        /// </summary>
        static public void TransformRoots(Scene scene, Matrix4x4? matrix) {
            if (!matrix.HasValue) {
                return;
            }

            scene.GetRootGameObjects(s_RootWorkList);
            foreach(var root in s_RootWorkList) {
                TransformRoot(root.transform, matrix);
            }
            s_RootWorkList.Clear();
        }

        /// <summary>
        /// Transforms the given transform by the given matrix.
        /// </summary>
        static public void TransformRoot(Transform transform, Matrix4x4? matrix) {
            if (!matrix.HasValue) {
                return;
            }

            Matrix4x4 mat = matrix.Value;
            TRS trs = new TRS(transform);
            trs.Position = mat.MultiplyPoint3x4(trs.Position);
            trs.Scale = mat.MultiplyVector(trs.Scale);
            trs.Rotation = mat.rotation * trs.Rotation;
            trs.CopyTo(transform);
        }

        #endregion // Utilities

#if UNITY_EDITOR

        [MenuItem("Field Day/Load All Scene Layers")]
        static public void EditorLoadInActiveScene() {
            EditorLoadInScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Field Day/Load All Scene Layers", validate = true)]
        static private bool EditorLoadInActiveScene_Validate() {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        static public void EditorLoadInScene(Scene scene) {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            List<ImportScene> imports = new List<ImportScene>();
            scene.GetAllComponents<ImportScene>(true, imports);
            if (imports.Count > 0) {
                Log.Msg("[ImportScene] Found {0} ImportScene objects in scene '{1}'", imports.Count, scene.path);
                foreach(var import in imports) {
                    bool shouldLoad = !import.Transform && import.Scene.IsValid;
                    if (shouldLoad) {
                        shouldLoad = import.Merge || (import.Flags & SceneImportFlags.Persistent) != 0;
                    }

                    if (!shouldLoad) {
                        continue;
                    }

                    Scene loaded = EditorSceneManager.GetSceneByPath(import.Scene.Path);
                    if (!loaded.isLoaded) {
                        Log.Msg("[ImportScene] Opening scene '{0}'...", import.Scene.Path);
                        EditorSceneManager.OpenScene(import.Scene.Path, OpenSceneMode.Additive);
                    };
                }
            }
        }

#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Scene import settings.
    /// </summary>
    public struct SceneImportSettings {
        public string Path;
        public SceneImportFlags Flags;
        public Matrix4x4? RootMatrix;
        public StringHash32 Tag;

        public SceneImportSettings(SceneReference reference, SceneImportFlags flags, StringHash32 tag = default) {
            Path = reference.Path;
            Flags = flags;
            RootMatrix = null;
            Tag = tag;
        }

        public SceneImportSettings(SceneReference reference, SceneImportFlags flags, Transform transformBy, StringHash32 tag = default) {
            Path = reference.Path;
            Flags = flags;
            RootMatrix = transformBy ? transformBy.localToWorldMatrix : null;
            Tag = tag;
        }

        public SceneImportSettings(SceneReference reference, SceneImportFlags flags, Matrix4x4 transformBy, StringHash32 tag = default) {
            Path = reference.Path;
            Flags = flags;
            RootMatrix = transformBy;
            Tag = tag;
        }

        public SceneType LoadType {
            get {
                if ((Flags & SceneImportFlags.Persistent) != 0) {
                    return SceneType.Persistent;
                } else {
                    return SceneType.Aux;
                }
            }
        }

        public Transform RootTransform {
            set {
                if (value == null) {
                    RootMatrix = null;
                } else {
                    RootMatrix = value.localToWorldMatrix;
                }
            }
        }
    }

    /// <summary>
    /// Dynamic scene import.
    /// </summary>
    public interface IDynamicSceneImport {
        IEnumerable<SceneImportSettings> GetSubscenes();
    }

    /// <summary>
    /// Scene import behavior flags.
    /// </summary>
    [Flags]
    public enum SceneImportFlags : uint {
        /// <summary>
        /// Additive scene load.
        /// By default, scenes will load additively at runtime.
        /// </summary>
        Auxillary = 0,

        /// <summary>
        /// Attaches the scene as a child of its parent.
        /// </summary>
        AttachAsChild = 0x01,
        
        /// <summary>
        /// Imports lighting data into the main scene.
        /// </summary>
        ImportLightingSettings = 0x02,

        /// <summary>
        /// The scene will not be unloaded when the next main scene is loaded.
        /// </summary>
        Persistent = 0x04,

        /// <summary>
        /// This scene will be reloaded, even if it is already loaded
        /// </summary>
        ForceReload = 0x08,

        /// <summary>
        /// Merge lightmaps into the main scene.
        /// </summary>
        MergeLightmaps = 0x10,
    }
}