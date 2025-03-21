using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;
using FieldDay.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace FieldDay.Layout {
    [CreateAssetMenu(menuName = "Field Day/Layout/Layout Package")]
    public sealed class CompressedLayoutPackage : NamedAsset, IEditorOnlyData {
        #region Types

        [Serializable]
        private struct TOCEntry {
            public SerializedHash32 Id;
            public uint Offset;
            public uint Length;
        }

        #endregion // Types

        #region Data

        [Header("Data")]
        [SerializeField] private TOCEntry[] m_TOC = Array.Empty<TOCEntry>();
        [SerializeField] private CompressedPackageBank m_Bank;
        [SerializeField] private byte[] m_CompressedData = Array.Empty<byte>();
        [SerializeField] private bool m_AllowLZCompression;

        #endregion // Data

        #region Package

#if UNITY_EDITOR
        [ContextMenu("Compress")]
        private void GatherAndCompress() {
            GameObject[] prefabsInDirectory = AssetUtility.Editor.FindAllAssets<GameObject>(PrefabPredicate, Path.GetDirectoryName(AssetDatabase.GetAssetPath(this)));
            List<GameObject> prefabsToBuild = new List<GameObject>(prefabsInDirectory.Length);

            foreach (var prefab in prefabsInDirectory) {
                if (prefab.name.Contains("Template")) {
                    continue;
                }
                prefabsToBuild.Add(prefab);
            }
            CompressedPackageBuilder bankBuilder = new CompressedPackageBuilder();
            List<TOCEntry> toc = new List<TOCEntry>();
            List<byte> allData = new List<byte>(4096);
            GameObject root = new GameObject("temp");
            root.SetActive(false);

            long rawSize = 0;
            long compressedSize = 0;

            try {
                using (Profiling.Time("compressing prefabs")) {
                    int idx = 0;
                    foreach (var prefab in prefabsToBuild) {
                        EditorUtility.DisplayProgressBar("Compressing Prefabs...", string.Format("Compressing '{0}' ({1}/{2})", prefab.name, idx + 1, prefabsToBuild.Count), (idx + 1) / (float)prefabsToBuild.Count);
                        idx++;
                        GameObject instantiated = GameObject.Instantiate(prefab, root.transform, false);
                        instantiated.name = prefab.name;
                        TRS trs = new TRS(prefab.transform);
                        trs.CopyTo(instantiated.transform);
                        try {
                            Log.Msg("[CompressedLayoutPackage] Encoding '{0}'", prefab.name);
                            byte[] compressed = instantiated.GetComponent<CompressiblePrefab>().Compress(bankBuilder, CompressedTransformBounds.Default, CompressedRectTransformBounds.Default);
                            rawSize += compressed.Length;
                            if (m_AllowLZCompression) {
                                Log.Msg("[LayoutPrefabPackage] Compressing '{0}'...", prefab.name);
                                byte[] lzBytes;
                                byte[] uncompressed = (byte[])compressed.Clone();
                                LZCompressionResult result = LZCompression.Compress(compressed, out lzBytes);
                                if (result == LZCompressionResult.Success) {
                                    Log.Msg("[CompressedLayoutPackage] Compression Ratio for '{0}': {1}", prefab.name, (float)uncompressed.Length / lzBytes.Length);
                                    byte[] decompressed;
                                    if (LZCompression.Decompress(lzBytes, out decompressed) != LZDecompressionResult.Success) {
                                        Log.Error("[LayoutPrefabPackage] Compressed data unable to be decompressed!");
                                    } else if (!ArrayUtils.ContentEquals(uncompressed, decompressed)) {
                                        Log.Error("[LayoutPrefabPackage] Compressed data, when uncompressed, is not identical");
                                    } else {
                                        compressed = lzBytes;
                                    }
                                }
                            }
                            TOCEntry entry = new TOCEntry() {
                                Id = prefab.name,
                                Offset = (uint)allData.Count,
                                Length = (uint)compressed.Length
                            };
                            compressedSize += compressed.Length;
                            toc.Add(entry);
                            allData.AddRange(compressed);
                        } finally {
                            GameObject.DestroyImmediate(instantiated);
                        }
                    }
                    m_Bank = new CompressedPackageBank(bankBuilder);
                    m_TOC = toc.ToArray();
                    m_CompressedData = allData.ToArray();

                    Log.Msg("[CompressedLayoutPackage] Total Compression Ratio: {0} ({1} raw vs {2} compressed)", (float)rawSize / compressedSize, rawSize, compressedSize);
                }

                EditorUtility.SetDirty(this);
            } finally {
                EditorUtility.ClearProgressBar();
                GameObject.DestroyImmediate(root);
            }
        }

        static private Predicate<GameObject> PrefabPredicate = (go) => {
            return go.GetComponent<CompressiblePrefab>();
        };
#endif // UNITY_EDITOR

        #endregion // Package

        #region IEditorOnlyData

#if UNITY_EDITOR

        void IEditorOnlyData.ClearEditorData(bool isDevelopmentBuild) {
            for (int i = 0; i < m_TOC.Length; i++) {
                EditorOnlyData.Strip(ref m_TOC[i].Id);
            }
        }

#endif // UNITY_EDITOR

        #endregion // IEditorOnlyData

        #region Inspector

#if UNITY_EDITOR

        [CustomEditor(typeof(CompressedLayoutPackage), true)]
        private class Inspector : UnityEditor.Editor {
            [NonSerialized] private GUIStyle m_Style;
            [NonSerialized] private SerializedProperty m_AllowLZCompression;

            protected void OnEnable() {
                GetType().GetProperty("alwaysAllowExpansion", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, true);
                m_AllowLZCompression = serializedObject.FindProperty("m_AllowLZCompression");
            }

            public override void OnInspectorGUI() {
                if (m_Style == null) {
                    m_Style = new GUIStyle("ScriptText");
                }

                serializedObject.UpdateIfRequiredOrScript();

                CompressedLayoutPackage prefabPackage = (CompressedLayoutPackage)target;

                if (GUILayout.Button("Build")) {
                    prefabPackage.GatherAndCompress();
                }
                EditorGUILayout.PropertyField(m_AllowLZCompression);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Total Size", EditorUtility.FormatBytes(prefabPackage.m_CompressedData.Length));
                EditorGUILayout.LabelField("Prefabs", prefabPackage.m_TOC.Length.ToString());

                if (prefabPackage.m_TOC.Length > 0) {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        foreach (var entry in prefabPackage.m_TOC) {
                            EditorGUILayout.LabelField(entry.Id.Source(), EditorUtility.FormatBytes(entry.Length));
                        }
                    }
                    EditorGUILayout.EndVertical();
                }

                if (prefabPackage.m_Bank != null) {
                    EditorGUILayout.LabelField("Strings", prefabPackage.m_Bank.StringTable.Length.ToString());
                    if (prefabPackage.m_Bank.StringTable.Length > 0) {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            using (new EditorGUI.DisabledScope(true)) {
                                foreach (var entry in prefabPackage.m_Bank.StringTable) {
                                    EditorGUILayout.TextField(entry);
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.LabelField("References", prefabPackage.m_Bank.AssetTable.Length.ToString());
                    if (prefabPackage.m_Bank.AssetTable.Length > 0) {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        {
                            using (new EditorGUI.DisabledScope(true)) {
                                foreach (var entry in prefabPackage.m_Bank.AssetTable) {
                                    EditorGUILayout.ObjectField(entry, typeof(UnityEngine.Object), false);
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif // UNITY_EDITOR

        #endregion // Inspector
    }
}