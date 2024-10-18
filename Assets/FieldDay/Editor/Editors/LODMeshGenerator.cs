#if HAS_MESHSIMPLIFIER

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using BeauUtil;
using UnityEditor;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityMeshSimplifier;

namespace FieldDay.Editor {
    public class LODMeshGenerator : EditorWindow {
        #region Save Location

        static private string LastSaveLocationString;

        #endregion // Save Location

        #region Public Properties

        public Mesh Source;
        [Range(0.01f, 1)] public float Quality1 = 0.5f;
        [Range(0, 1)] public float Quality2 = 0.2f;

        // generation settings
        [Range(0.1f, 16)] public float Aggressiveness = 7;
        public bool PreserveBorderEdges = false;
        public bool PreserveUVSeamEdges = false;
        public bool PreserveUVFoldoverEdges = false;
        public bool PreserveSurfaceCurvature = true;
        public bool PreserveBlendShapes = true;
        public bool PreserveSkinningWeights = true;

        // preview
        public Material RenderMaterial;

        #endregion // Public Properties

        #region State

        [NonSerialized] private MeshPreview m_SourcePreview;
        [NonSerialized] private MeshPreview m_LOD1Preview;
        [NonSerialized] private MeshPreview m_LOD2Preview;

        [NonSerialized] private Mesh m_LOD1;
        [NonSerialized] private Mesh m_LOD2;

        [SerializeField] private Vector2 m_Scroll;
        [SerializeField] private Vector2 m_PreviewDir;
        [SerializeField] private float m_ZoomFactor = 1;
        [SerializeField] private Vector3 m_PivotOffset;
        [SerializeField] private Vector2 m_LightDir;

        private SerializedObject m_SerializedObject;
        private SerializedProperty m_SourceProperty;
        private SerializedProperty m_Quality1Property;
        private SerializedProperty m_Quality2Property;
        private SerializedProperty m_AggressivenessProperty;
        private SerializedProperty m_PreserveBorderEdgesProperty;
        private SerializedProperty m_PreserveUVSeamEdgesProperty;
        private SerializedProperty m_PreserveUVFoldoverEdgesProperty;
        private SerializedProperty m_PreserveSurfaceCurvatureProperty;
        private SerializedProperty m_PreserveBlendShapesProperty;
        private SerializedProperty m_PreserveSkinningWeightsProperty;
        private SerializedProperty m_RenderMaterialProperty;

        #endregion // State

        #region Shared

        static private GUIStyle s_LODInfoStyle;
        static private GUIStyle s_LODRenderBoxStyle;
        static private GUIContent s_SharedContent;
        static private Material s_DefaultMaterial;

        static private FieldInfo s_MeshPreviewSettings;
        static private FieldInfo s_MeshPreviewPreviewer;

        #endregion // Shared

        #region Unity Events

        private void OnEnable() {
            m_SerializedObject = new SerializedObject(this);

            m_SourceProperty = m_SerializedObject.FindProperty("Source");
            m_Quality1Property = m_SerializedObject.FindProperty("Quality1");
            m_Quality2Property = m_SerializedObject.FindProperty("Quality2");

            m_AggressivenessProperty = m_SerializedObject.FindProperty("Aggressiveness");
            m_PreserveBorderEdgesProperty = m_SerializedObject.FindProperty("PreserveBorderEdges");
            m_PreserveUVSeamEdgesProperty = m_SerializedObject.FindProperty("PreserveUVSeamEdges");
            m_PreserveUVFoldoverEdgesProperty = m_SerializedObject.FindProperty("PreserveUVFoldoverEdges");
            m_PreserveSurfaceCurvatureProperty = m_SerializedObject.FindProperty("PreserveSurfaceCurvature");
            m_PreserveBlendShapesProperty = m_SerializedObject.FindProperty("PreserveBlendShapes");
            m_PreserveSkinningWeightsProperty = m_SerializedObject.FindProperty("PreserveSkinningWeights");

            m_RenderMaterialProperty = m_SerializedObject.FindProperty("RenderMaterial");

            minSize = new Vector2(1280, 600);
            titleContent = new GUIContent("LOD Mesh Generator");
        }

        private void OnDisable() {
            Ref.TryDispose(ref m_SourcePreview);
            Ref.TryDispose(ref m_LOD1Preview);
            Ref.TryDispose(ref m_LOD2Preview);
        }

        private void OnDestroy() {
            DestroyResource(ref m_LOD1);
            DestroyResource(ref m_LOD2);
        }

        #endregion // Unity Events

        #region Editor

        [MenuItem("Window/Field Day/LOD Generator")]
        static private void Create() {
            var window = EditorWindow.GetWindow<LODMeshGenerator>();
            window.Show();
        }

        private void OnGUI() {
            SharedInit();

            m_SerializedObject.UpdateIfRequiredOrScript();

            using (new EditorGUILayout.HorizontalScope(s_LODRenderBoxStyle)) {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_SourceProperty, TempContent("Source Mesh"));
                    if (EditorGUI.EndChangeCheck()) {
                        DestroyResource(ref m_LOD1);
                        DestroyResource(ref m_LOD2);
                    }
                    EditorGUILayout.PropertyField(m_Quality1Property, TempContent("LOD 1 Quality"));
                    EditorGUILayout.PropertyField(m_Quality2Property, TempContent("LOD 2 Quality"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_AggressivenessProperty, TempContent("Aggressiveness"));
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.PrefixLabel("Preserve");
                        EditorGUILayout.PropertyField(m_PreserveSurfaceCurvatureProperty, TempContent("Surface Curvature"));
                        EditorGUILayout.PropertyField(m_PreserveBorderEdgesProperty, TempContent("Border Edges"));
                        EditorGUILayout.PropertyField(m_PreserveUVSeamEdgesProperty, TempContent("UV Seam Edges"));
                        EditorGUILayout.PropertyField(m_PreserveUVFoldoverEdgesProperty, TempContent("UV Foldover Edges"));
                    }
                    using (new EditorGUILayout.HorizontalScope()) {
                        EditorGUILayout.PrefixLabel("Preserve");
                        EditorGUILayout.PropertyField(m_PreserveBlendShapesProperty, TempContent("Blend Shapes"));
                        EditorGUILayout.PropertyField(m_PreserveSkinningWeightsProperty, TempContent("Skinning"));
                    }

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_RenderMaterialProperty, TempContent("Preview Material"));

                    m_SerializedObject.ApplyModifiedProperties();
                }
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(300))) {
                    using (new EditorGUI.DisabledScope(Source == null)) {
                        if (GUILayout.Button("Preview")) {
                            GenerateAllMeshes();
                        }
                    }
                    using (new EditorGUI.DisabledScope(Source == null || (!m_LOD1 && !m_LOD2))) {
                        if (GUILayout.Button("Save")) {

                        }
                    }
                }
            }

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, false, false);
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true))) {
                    if (Source != null) {
                        RenderMeshPreview(ref m_SourcePreview, Source, Source, 0, RenderMaterial, "LOD 0:\n", ref m_PreviewDir, ref m_ZoomFactor, ref m_PivotOffset, ref m_LightDir);
                    }
                    if (m_LOD1 != null) {
                        RenderMeshPreview(ref m_LOD1Preview, Source, m_LOD1, 1, RenderMaterial, "LOD 1:\n", ref m_PreviewDir, ref m_ZoomFactor, ref m_PivotOffset, ref m_LightDir);
                    }
                    if (m_LOD2 != null) {
                        RenderMeshPreview(ref m_LOD2Preview, Source, m_LOD2, 2, RenderMaterial, "LOD 2:\n", ref m_PreviewDir, ref m_ZoomFactor, ref m_PivotOffset, ref m_LightDir);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            m_SerializedObject.ApplyModifiedProperties();

            if (Source || m_LOD1 || m_LOD2) {
                // Repaint();
            }
        }

        private void GenerateAllMeshes() {
            if (!Source) {
                return;
            }

            SimplificationOptions opt = SimplificationOptions.Default;
            opt.Agressiveness = Aggressiveness;
            opt.PreserveBorderEdges = PreserveBorderEdges;
            opt.PreserveUVSeamEdges = PreserveUVSeamEdges;
            opt.PreserveUVFoldoverEdges = PreserveUVFoldoverEdges;
            opt.PreserveSurfaceCurvature = PreserveSurfaceCurvature;

            MeshDataClearFlags clearFlags = 0;
            if (!PreserveBlendShapes) {
                clearFlags |= MeshDataClearFlags.BlendShapes;
            }
            if (!PreserveSkinningWeights) {
                clearFlags |= MeshDataClearFlags.SkinningWeights;
            }

            try {
                EditorUtility.DisplayProgressBar("Generating LOD Meshes", "LOD 1...", 0);
                GenerateMesh(opt, Source, Quality1, clearFlags, ref m_LOD1);
                EditorUtility.DisplayProgressBar("Generating LOD Meshes", "LOD 2...", 0.5f);
                GenerateMesh(opt, Source, Quality2, clearFlags, ref m_LOD2);
            } finally {
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion // Editor

        #region Operations

        static private void GenerateMesh(SimplificationOptions simplificationOptions, Mesh source, float quality, MeshDataClearFlags clearFlags, ref Mesh lod) {
            DestroyResource(ref lod);

            if (quality > 0) {
                MeshSimplifier simplifier = new MeshSimplifier(source);
                simplifier.SimplificationOptions = simplificationOptions;

                if (quality >= 1) {
                    simplifier.SimplifyMeshLossless();
                } else {
                    simplifier.SimplifyMesh(quality);
                }
                if ((clearFlags & MeshDataClearFlags.BlendShapes) != 0) {
                    simplifier.ClearBlendShapes();
                }
                if ((clearFlags & MeshDataClearFlags.SkinningWeights) != 0) {
                    simplifier.BoneWeights = Array.Empty<BoneWeight>();
                }

                lod = simplifier.ToMesh();

                if ((clearFlags & MeshDataClearFlags.SkinningWeights) != 0) {
                    lod.bindposes = Array.Empty<Matrix4x4>();
                }

                if (clearFlags == 0) {
                    // attempt to preserve same vertex format
                    lod.bindposes = source.bindposes;
                }

                lod.UploadMeshData(true);
            }
        }

        static private bool SaveResourceAs(Mesh mesh, string name) {
            string lastDirectory = EditorPrefs.GetString(LastSaveLocationString, "Assets/");
            string path = EditorUtility.SaveFilePanelInProject("Save LOD Mesh", name, "mesh", "Save this mesh", lastDirectory);
            if (!string.IsNullOrEmpty(path)) {
                Mesh clone = Instantiate(mesh);
                clone.name = Path.GetFileNameWithoutExtension(path);
                AssetDatabase.CreateAsset(clone, path);
                lastDirectory = Path.GetDirectoryName(path);
                EditorPrefs.SetString(LastSaveLocationString, lastDirectory);
                return true;
            }

            return false;
        }

        static private void DestroyResource<T>(ref T obj) where T : UnityEngine.Object {
            if (obj != null) {
                DestroyImmediate(obj);
                obj = null;
            }
        }

        #endregion // Operations

        #region Mesh Preview

        static private void RenderMeshPreview(ref MeshPreview previewer, Mesh sourceMesh, Mesh targetMesh, int lodLevel, Material renderMaterial, string text, ref Vector2 direction, ref float zoom, ref Vector3 pivotOffset, ref Vector2 lightDir) {
            if (!renderMaterial) {
                renderMaterial = s_DefaultMaterial;
            }

            if (previewer == null) {
                previewer = new MeshPreview(targetMesh);
            } else {
                previewer.mesh = targetMesh;
            }

            SettingsWrapper settings = new SettingsWrapper(s_MeshPreviewSettings.GetValue(previewer));

            if (Event.current.type == EventType.Repaint) {
                settings.activeMaterial = renderMaterial;
            }

            Rect r = GUILayoutUtility.GetRect(300, 300, GUILayout.ExpandHeight(true));
            GUI.Box(r, "", s_LODRenderBoxStyle);
            r.x += 4;
            r.y += 4;
            r.width -= 8;
            r.height -= 8;

            if (direction == default(Vector2)) {
                direction = settings.previewDir;
            } else {
                settings.previewDir = direction;
            }

            if (pivotOffset == default(Vector3)) {
                pivotOffset = settings.pivotPositionOffset;
            } else {
                settings.pivotPositionOffset = pivotOffset;
            }

            if (lightDir == default(Vector2)) {
                lightDir = settings.lightDir;
            } else {
                settings.lightDir = lightDir;
            }

            settings.zoomFactor = zoom;

            Rect editorRect = r;
            editorRect.height -= 24;

            bool prevRenderPipeline = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = true;

            if (Event.current.type == EventType.Repaint) {
                RenderPreviewWithSRP(previewer, settings, editorRect, EditorStyles.helpBox);
            } else {
                previewer.OnPreviewGUI(editorRect, EditorStyles.helpBox);
            }

            Unsupported.useScriptableRenderPipeline = prevRenderPipeline;

            direction = settings.previewDir;
            zoom = settings.zoomFactor;
            pivotOffset = settings.pivotPositionOffset;
            lightDir = settings.lightDir;

            if (Event.current.type == EventType.Repaint) {
                Rect labelRect = r;
                labelRect.height = 100;
                GUI.Label(labelRect, text + MeshPreview.GetInfoString(targetMesh), s_LODInfoStyle);
            }

            Rect saveAsButton = default;
            saveAsButton.x = r.xMax - 200;
            saveAsButton.y = r.yMax - 20;
            saveAsButton.width = 200;
            saveAsButton.height = 20;

            if (targetMesh != sourceMesh) {
                if (GUI.Button(saveAsButton, "Save As")) {
                    string sourceName = sourceMesh.name + "_LOD" + lodLevel.ToString();
                    SaveResourceAs(targetMesh, sourceName);
                }
            }
        }

        static private void RenderPreviewWithSRP(MeshPreview preview, SettingsWrapper settings, Rect rect, GUIStyle background) {
            PreviewRenderUtility previewUtility = (PreviewRenderUtility) s_MeshPreviewPreviewer.GetValue(preview);
            previewUtility.BeginPreview(rect, background);

            Bounds bounds = preview.mesh.bounds;
            Transform component = previewUtility.camera.GetComponent<Transform>();
            previewUtility.camera.nearClipPlane = 0.0001f;
            previewUtility.camera.farClipPlane = 1000f;
            float magnitude = bounds.extents.magnitude;
            float num = 4f * magnitude;
            previewUtility.camera.orthographic = false;
            Quaternion identity = Quaternion.identity;
            Vector3 position = identity * Vector3.forward * ((0f - num) * settings.zoomFactor) + settings.pivotPositionOffset;
            component.position = position;
            component.rotation = identity;

            Vector2 lightDir = settings.lightDir;

            previewUtility.lights[0].intensity = 1.1f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(0f - lightDir.y, 0f - lightDir.x, 0f);
            previewUtility.lights[1].intensity = 1.1f;
            previewUtility.lights[1].transform.rotation = Quaternion.Euler(lightDir.y, lightDir.x, 0f);
            previewUtility.ambientColor = new Color(0.1f, 0.1f, 0.1f, 0f);

            RenderMeshPreviewSkipCameraAndLighting(preview.mesh, bounds, previewUtility, settings, null);

            previewUtility.EndAndDrawPreview(rect);
        }

        static private void RenderMeshPreviewSkipCameraAndLighting(Mesh mesh, Bounds bounds, PreviewRenderUtility previewUtility, SettingsWrapper settings, MaterialPropertyBlock customProperties) {
            Vector2 previewDir = settings.previewDir;
            Quaternion quaternion = Quaternion.Euler(previewDir.y, 0f, 0f) * Quaternion.Euler(0f, previewDir.x, 0f);
            Vector3 pos = quaternion * -bounds.center;
            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(fog: false);
            int subMeshCount = mesh.subMeshCount;

            Material activeMaterial = settings.activeMaterial;
            Material wireMaterial = settings.wireMaterial;

            if (activeMaterial != null) {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                for (int i = 0; i < subMeshCount; i++) {
                    previewUtility.DrawMesh(mesh, pos, quaternion, activeMaterial, i, customProperties);
                }
                previewUtility.Render(true);
            }

            if (wireMaterial != null) {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                GL.wireframe = true;
                for (int j = 0; j < subMeshCount; j++) {
                    MeshTopology topology = mesh.GetTopology(j);
                    if (topology != MeshTopology.Lines && topology != MeshTopology.LineStrip && topology != MeshTopology.Points) {
                        previewUtility.DrawMesh(mesh, pos, quaternion, wireMaterial, j, customProperties);
                    }
                }
                previewUtility.Render(true);
                GL.wireframe = false;
            }
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);
        }

        #endregion // Mesh Preview

        #region Shared

        static private GUIContent TempContent(string label) {
            if (s_SharedContent == null) {
                s_SharedContent = new GUIContent(label);
            } else {
                s_SharedContent.text = label;
            }
            return s_SharedContent;
        }

        static private void SharedInit() {
            if (s_LODInfoStyle == null) {
                s_LODInfoStyle = new GUIStyle(EditorStyles.label);
                s_LODInfoStyle.fontStyle = FontStyle.Bold;
                s_LODInfoStyle.alignment = TextAnchor.UpperLeft;
                s_LODInfoStyle.margin = new RectOffset(4, 4, 4, 4);

                s_LODRenderBoxStyle = new GUIStyle(EditorStyles.helpBox);
            }

            if (s_SharedContent == null) {
                s_SharedContent = new GUIContent();
            }

            if (LastSaveLocationString == null) {
                LastSaveLocationString = PlayerSettings.productGUID + "/LastLODSaveDirectory";
            }

            if (s_DefaultMaterial == null) {
                s_DefaultMaterial = (Material) typeof(Material).GetMethod("GetDefaultMaterial", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            }

            if (s_MeshPreviewSettings == null) {
                Type meshPreviewType = typeof(MeshPreview);
                s_MeshPreviewSettings = meshPreviewType.GetField("m_Settings", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                s_MeshPreviewPreviewer = meshPreviewType.GetField("m_PreviewUtility", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                Type meshPreviewSettingsType = meshPreviewType.GetNestedType("Settings", BindingFlags.NonPublic);
                SettingsWrapper.SettingsType = meshPreviewSettingsType;
            }
        }

        private struct SettingsWrapper {
            private readonly object m_InternalObj;

            public SettingsWrapper(object obj) {
                m_InternalObj = obj;
            }

            public T Get<T>(string propName) {
                if (m_InternalObj != null) {
                    PropertyInfo p = s_CachedProperties.Read(propName);
                    return (T) p.GetValue(m_InternalObj);
                } else {
                    return default(T);
                }
            }

            public void Set<T>(string propName, T value) {
                if (m_InternalObj != null) {
                    PropertyInfo p = s_CachedProperties.Read(propName);
                    p.SetValue(m_InternalObj, value);
                }
            }

            public Vector2 previewDir {
                get { return Get<Vector2>("previewDir"); }
                set { Set<Vector2>("previewDir", value); }
            }

            public float zoomFactor {
                get { return Get<float>("zoomFactor"); }
                set { Set<float>("zoomFactor", value); }
            }

            public Material activeMaterial {
                get { return Get<Material>("activeMaterial"); }
                set { Set<Material>("activeMaterial", value); }
            }

            public Material wireMaterial {
                get { return Get<Material>("wireMaterial"); }
                set { Set<Material>("wireMaterial", value); }
            }

            public Vector3 pivotPositionOffset {
                get { return Get<Vector3>("pivotPositionOffset"); }
                set { Set<Vector3>("pivotPositionOffset", value); }
            }

            public Vector2 lightDir {
                get { return Get<Vector2>("lightDir"); }
                set { Set<Vector2>("lightDir", value); }
            }

            static internal Type SettingsType;

            static private LruCache<StringHash32, PropertyInfo> s_CachedProperties = new LruCache<StringHash32, PropertyInfo>(16, new CacheCallbacks<StringHash32, PropertyInfo>() {
                Fetch = (k) => {
                    return SettingsType.GetProperty(k.ToDebugString(), BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                }
            });
        }

        #endregion // Shared

        [Flags]
        private enum MeshDataClearFlags {
            BlendShapes = 0x01,
            SkinningWeights = 0x02
        }
    }
}

#endif // HAS_MESHSIMPLIFIER