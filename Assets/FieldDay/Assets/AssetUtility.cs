using UnityEngine;
using System;
using BeauUtil;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Assets {
    /// <summary>
    /// Asset utility methods.
    /// </summary>
    static public class AssetUtility {
        /// <summary>
        /// Manually unloads the given object.
        /// </summary>
        static public void ManualUnload(UnityEngine.Object obj) {
            if (!ReferenceEquals(obj, null)) {
                if (IsPersistent(obj)) {
                    Debug.LogFormat("[AssetUtility] Manually unloading persistent object '{0}'", obj.name);
                    Resources.UnloadAsset(obj);
                } else {
                    Debug.LogFormat("[AssetUtility] Manually unloading object '{0}'", obj.name);
#if UNITY_EDITOR
                    UnityEngine.Object.Destroy(obj);
#else
                    UnityEngine.Object.DestroyImmediate(obj, true);
#endif // UNITY_EDITOR
                }
            }
        }

        /// <summary>
        /// Manually destroys the given asset.
        /// Use carefully! In builds you won't get this asset back.
        /// </summary>
        static public void DestroyAsset(UnityEngine.Object asset) {
            if (!ReferenceEquals(asset, null)) {
                Assert.True(IsPersistent(asset), "Asset is not persistent");
                Debug.LogWarningFormat("[AssetUtility] Manually destroying asset '{0}'!", asset.name);
#if !UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(asset, true);
#else
                Resources.UnloadAsset(asset);
#endif // UNITY_EDITOR
            }
        }

        /// <summary>
        /// Manually destroys the given asset.
        /// Use carefully! In builds you won't get this asset back.
        /// </summary>
        static public void DestroyAsset<T>(ref T asset) where T : UnityEngine.Object {
            if (!ReferenceEquals(asset, null)) {
                Assert.True(IsPersistent(asset), "Asset is not persistent");
                Debug.LogWarningFormat("[AssetUtility] Manually destroying asset '{0}'!", asset.name);
#if !UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(asset, true);
                asset = null;
#else
                Resources.UnloadAsset(asset);
#endif // UNITY_EDITOR
            }
        }

        /// <summary>
        /// Unloads unused assets.
        /// Returns the async operation if asynchronous.
        /// </summary>
        static public AsyncOperation UnloadUnused() {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.UnloadUnusedAssetsImmediate(true);
                return null;
            }
#endif // UNITY_EDITOR
            return Resources.UnloadUnusedAssets();
        }

        /// <summary>
        /// Returns if the given asset is persistent.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsPersistent(UnityEngine.Object obj) {
            return UnityHelper.IsPersistent(obj);
        }

        /// <summary>
        /// Caches the name hash of the given object.
        /// </summary>
        static public StringHash32 CacheNameHash(ref StringHash32 hash, UnityEngine.Object obj) {
            if (hash.IsEmpty) {
                hash = obj.name;
            }
            return hash;
        }

        /// <summary>
        /// Caches the name hash of the given object.
        /// </summary>
        static public StringHash32 CacheNameHash(ref StringHash32 hash, object asset) {
            if (hash.IsEmpty) {
                hash = NameOf(asset);
            }
            return hash;
        }

        /// <summary>
        /// Id of the given named asset.
        /// </summary>
        static public StringHash32 IdOf(NamedAsset asset) {
            if (asset == null) {
                return StringHash32.Null;
            }

            return asset.AssetId;
        }

        /// <summary>
        /// Returns the name of the given object.
        /// </summary>
        static public string NameOf(UnityEngine.Object obj) {
            if (obj == null) {
                return null;
            }

            return obj.name;
        }

        /// <summary>
        /// Returns the name of the given object.
        /// </summary>
        static public string NameOf(object asset) {
            if (asset == null) {
                return null;
            }

            UnityEngine.Object obj = asset as UnityEngine.Object;
            if (obj != null) {
                return obj.name;
            }

            return asset.ToString();
        }

        /// <summary>
        /// Adds a reference to the given asset.
        /// If this is an IRefCountedAsset, this will only return true on the first reference.
        /// Otherwise, this will always return true;
        /// </summary>
        static public bool AddReference(object asset) {
            IRefCountedAsset counted = asset as IRefCountedAsset;
            if (counted != null) {
                return counted.AddRef();
            } else {
                return true;
            }
        }

        /// <summary>
        /// Adds a reference to the given asset.
        /// If this is an IRefCountedAsset, this will only return true on the first reference.
        /// Otherwise, this will always return true;
        /// </summary>
        static public bool AddReference(IRefCountedAsset asset) {
            return asset.AddRef();
        }

        /// <summary>
        /// Removes a reference from the given asset.
        /// If this is an IRefCountedAsset, this will only return true on the last dereference.
        /// Otherwise, this will always return true;
        /// </summary>
        static public bool RemoveReference(object asset) {
            IRefCountedAsset counted = asset as IRefCountedAsset;
            if (counted != null) {
                return counted.RemoveRef();
            } else {
                return true;
            }
        }

        /// <summary>
        /// Removes a reference from the given asset.
        /// If this is an IRefCountedAsset, this will only return true on the last dereference.
        /// Otherwise, this will always return true;
        /// </summary>
        static public bool RemoveReference(IRefCountedAsset asset) {
            return asset.RemoveRef();
        }

        /// <summary>
        /// Editor utilities.
        /// </summary>
        static public class Editor {
#if UNITY_EDITOR
            static public T FindAsset<T>() where T : UnityEngine.Object {
                string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)));
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        T asset = obj as T;
                        if (asset)
                            return asset;
                    }
                }

                return null;
            }

            static public T FindAsset<T>(string name) where T : UnityEngine.Object {
                string[] assetGuids = AssetDatabase.FindAssets(name + " " + NameFilter(typeof(T)));
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        T asset = obj as T;
                        if (asset && asset.name == name)
                            return asset;
                    }
                }

                return null;
            }

            static public T FindAsset<T>(StringHash32 id) where T : UnityEngine.Object {
                string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)));
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        T asset = obj as T;
                        if (asset && asset.name == id)
                            return asset;
                    }
                }

                return null;
            }

            static public SceneAsset FindScene(string name) {
                string[] assetGuids = AssetDatabase.FindAssets("t:SceneAsset");
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    var obj = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (obj.name != name)
                        continue;
                    return obj;
                }

                return null;
            }

            static public T FindPrefab<T>(string name) where T : Component {
                string[] assetGuids = AssetDatabase.FindAssets("t:GameObject");
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (obj.name != name)
                        continue;
                    T component = obj.GetComponent<T>();
                    if (component)
                        return component;
                }

                return null;
            }

            static public T FindPrefab<T>(string name, params string[] directories) where T : Component {
                string[] assetGuids = AssetDatabase.FindAssets("t:GameObject", directories);
                if (assetGuids == null)
                    return null;

                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (obj.name != name)
                        continue;
                    T component = obj.GetComponent<T>();
                    if (component)
                        return component;
                }

                return null;
            }

            static public T[] FindAllAssets<T>(params string[] directories) where T : UnityEngine.Object {
                if (directories.Length == 0)
                    directories = null;

                string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)), directories);
                if (assetGuids == null)
                    return null;

                HashSet<T> assets = new HashSet<T>();
                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        T asset = obj as T;
                        if (asset)
                            assets.Add(asset);
                    }
                }

                T[] arr = new T[assets.Count];
                assets.CopyTo(arr);
                return arr;
            }

            static public T[] FindAllAssets<T>(Predicate<T> predicate, params string[] directories) where T : UnityEngine.Object {
                if (directories.Length == 0)
                    directories = null;

                string[] assetGuids = AssetDatabase.FindAssets(NameFilter(typeof(T)), directories);
                if (assetGuids == null)
                    return null;

                HashSet<T> assets = new HashSet<T>();
                for (int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        T asset = obj as T;
                        if (asset && predicate(asset))
                            assets.Add(asset);
                    }
                }

                T[] arr = new T[assets.Count];
                assets.CopyTo(arr);
                return arr;
            }

            static public TextAsset[] FindAllTextFilesByExtension(string extension, params string[] directories) {
                if (directories.Length == 0)
                    directories = null;

                string[] assetGuids = AssetDatabase.FindAssets("t:TextAsset", directories);
                if (assetGuids == null)
                    return null;

                HashSet<TextAsset> assets = new HashSet<TextAsset>();
                for(int i = 0; i < assetGuids.Length; i++) {
                    string path = AssetDatabase.GUIDToAssetPath(assetGuids[i]);
                    if (!path.EndsWith(extension))
                        continue;

                    foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path)) {
                        TextAsset asset = obj as TextAsset;
                        if (asset) {
                            assets.Add(asset);
                        }
                    }
                }

                TextAsset[] arr = new TextAsset[assets.Count];
                assets.CopyTo(arr);
                return arr;
            }

            static public readonly Predicate<UnityEngine.Object> IgnoreTemplates = (o) => {
                return char.IsLetterOrDigit(o.name[0]);
            };

            static private string NameFilter(Type type) {
                string fullname = type.FullName;
                if (fullname.StartsWith("UnityEngine.") || fullname.StartsWith("UnityEditor.")) {
                    fullname = fullname.Substring(12);
                }
                return "t:" + fullname;
            }
#endif // UNITY_EDITOR
        }
    }

    /// <summary>
    /// Delegate for looking up an asset's id from itself.
    /// </summary>
    public delegate StringHash32 AssetKeyFunction<T>(in T asset);
}