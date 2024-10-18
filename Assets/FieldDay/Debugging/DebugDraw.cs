#if (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // (UNITY_EDITOR && !IGNORE_UNITY_EDITOR) || DEVELOPMENT_BUILD

#if USING_XR && !UNITY_EDITOR
#define SKIP_ONGUI
#endif // USING_XR && !UNITY_EDITOR

using System;
using BeauUtil;
using UnityEngine;
using System.Diagnostics;
using BeauUtil.Debugger;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

using Debug = UnityEngine.Debug;

namespace FieldDay.Debugging {
    /// <summary>
    /// Debug rendering helper.
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class DebugDraw : MonoBehaviour {
#if DEVELOPMENT

        #region Types

        private enum EnableMode {
            Enabled,
            Disabled,
            DisableInBuildOnly
        }

        private struct DrawParams {
            public Color32 Color;
            public float LineWidth;
            public bool DepthTest;
            public sbyte Category;
        }

        private struct DrawState {
            public float Duration;
        }

        private struct Vector3x2RenderState {
            public DrawParams Params;
            public DrawState State;

            public Vector3 Min;
            public Vector3 Max;
        }

        private struct SphereRenderState {
            public DrawParams Params;
            public DrawState State;

            public Vector3 Center;
            public float Radius;
            public bool Solid;
        }

        private struct TextRenderState {
            public DrawParams Params;
            public DrawState State;

            public Vector3 Position;
            public Vector2 Offset;
            public bool WorldSpace;
            public string Text;
            public TextAnchor Alignment;
            public DebugTextStyle Style;
        }

        #endregion // Types

        #region Buffers

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DebugVertexFormat {
            [VertexAttr(VertexAttribute.Position)] public Vector3 Position;
            [VertexAttr(VertexAttribute.Color)] public Color32 Color;
        }

        #endregion // Buffers

        #region Inspector

        [SerializeField] private EnableMode m_EnableMode = EnableMode.DisableInBuildOnly;

        [Header("Mesh Rendering")]
        [SerializeField] private Font m_TextFont = null;
        [SerializeField] private Mesh m_SphereMesh = null;
        [SerializeField] private Mesh m_CubeMesh = null;
        [SerializeField] private float m_LineWidthToWorldScale = 0.08f;

        [Header("Materials")]
        [SerializeField] private Material m_DepthTestMaterial = null;
        [SerializeField] private Material m_OverlayMaterial = null;

        #endregion // Inspector

        [NonSerialized] private Mesh m_MainMesh;
        [NonSerialized] private Mesh m_OverlayMesh;
        [NonSerialized] private MeshData16<DebugVertexFormat> m_MainMeshData;
        [NonSerialized] private MeshData16<DebugVertexFormat> m_OverlayMeshData;
        [NonSerialized] private GUIStyle m_TextStylePlain;
        [NonSerialized] private GUIStyle m_TextStyleBox;
        [NonSerialized] private GUIContent m_TextContent;
        [NonSerialized] private float m_SphereMeshDefaultRadius;
        [NonSerialized] private float m_CubeMeshDefaultSize;
        [NonSerialized] private MaterialPropertyBlock m_TempMaterialPropertyBlock;

        static private RingBuffer<Vector3x2RenderState> s_ActiveLines = new RingBuffer<Vector3x2RenderState>();
        static private RingBuffer<Vector3x2RenderState> s_ActiveBoxes = new RingBuffer<Vector3x2RenderState>();
        static private RingBuffer<SphereRenderState> s_ActiveSpheres = new RingBuffer<SphereRenderState>();
        static private RingBuffer<TextRenderState> s_ActiveTexts = new RingBuffer<TextRenderState>();

        [NonSerialized] static private BitSet64 s_CategoryMask = new BitSet64();
        [NonSerialized] static private DebugDraw s_Instance;
        [NonSerialized] static private Camera s_MainCameraOverride;
        [NonSerialized] static private bool s_PauseAll = false;

        [NonSerialized] private bool m_InitializedResources = false;

        #region Unity Events

        private void Awake() {
            if (s_Instance != null && s_Instance != this) {
                Debug.LogWarning("[DebugDraw] Duplicate instances of DebugDraw");
                DestroyImmediate(this);
                return;
            }

            s_Instance = this;
            useGUILayout = false;

            m_MainMesh = CreateVolatileMesh("DEBUG_DepthTest");
            m_OverlayMesh = CreateVolatileMesh("DEBUG_Overlay");

            m_SphereMeshDefaultRadius = m_SphereMesh.bounds.size.y / 2;
            m_CubeMeshDefaultSize = m_CubeMesh.bounds.size.x;
            m_TempMaterialPropertyBlock = new MaterialPropertyBlock();

            m_MainMeshData = new MeshData16<DebugVertexFormat>(512);
            m_OverlayMeshData = new MeshData16<DebugVertexFormat>(512);

            switch (m_EnableMode) {
                case EnableMode.Disabled: {
                    s_PauseAll = true;
                    break;
                }
                case EnableMode.DisableInBuildOnly: {
                    s_PauseAll |= !Application.isEditor;
                    break;
                }
            }

#if UNITY_EDITOR
            SceneView.duringSceneGui += OnSceneGUI;            
#endif // UNITY_EDITOR
        }

        private void OnDestroy() {
            if (s_Instance != this) {
                return;
            }

            s_Instance = null;

            UnityHelper.SafeDestroy(ref m_MainMesh);
            UnityHelper.SafeDestroy(ref m_OverlayMesh);
            m_TempMaterialPropertyBlock.Clear();

#if UNITY_EDITOR
            SceneView.duringSceneGui -= OnSceneGUI;
#endif // UNITY_EDITOR
        }

        private void LateUpdate() {
            if (!enabled) {
                return;
            }

            float deltaTime = Math.Min(Time.unscaledDeltaTime, 0.1f);

            m_MainMeshData.Clear();
            m_OverlayMeshData.Clear();

            Camera mainCam = s_MainCameraOverride ? s_MainCameraOverride : Camera.main;
            if (mainCam) {
                RenderLines(deltaTime, mainCam.transform.forward, s_CategoryMask, !s_PauseAll);
            }
            RenderSpheres(deltaTime, s_CategoryMask, !s_PauseAll);

            if (m_MainMeshData.VertexCount > 0) {
                RenderParams p = new RenderParams(m_DepthTestMaterial);
                m_MainMeshData.Flush(m_MainMesh);
                Graphics.RenderMesh(p, m_MainMesh, 0, Matrix4x4.identity);
            }

            if (m_OverlayMeshData.VertexCount > 0) {
                RenderParams p = new RenderParams(m_OverlayMaterial);
                m_OverlayMeshData.Flush(m_OverlayMesh);
                Graphics.RenderMesh(p, m_OverlayMesh, 0, Matrix4x4.identity);
            }
        }

#if !SKIP_ONGUI

        private void OnGUI() {
            if (Event.current.type != EventType.Repaint) {
                return;
            }

#if UNITY_EDITOR
            if (FrameDebugger.enabled) {
                return;
            }
#endif // UNITY_EDITOR

            GUI.matrix = Matrix4x4.identity;
            float deltaTime = Math.Min(Time.unscaledDeltaTime, 0.1f);

            EnsureGUIResources();

            Camera mainCam = s_MainCameraOverride ? s_MainCameraOverride : Game.Rendering.PrimaryCamera;
            if (!mainCam) {
                mainCam = Camera.main;
            }
            if (mainCam) {
                RenderText(deltaTime, mainCam, s_CategoryMask, !s_PauseAll);
            } else {
                DecayText(deltaTime); 
            }
        }

#endif // !SKIP_ONGUI

#if UNITY_EDITOR

        private void OnSceneGUI(SceneView view) {
            if (!enabled) {
                return;
            }

            if (FrameDebugger.enabled) {
                return;
            }

            Handles.BeginGUI();

            EnsureGUIResources();
            RenderText(0, view.camera, s_CategoryMask, !s_PauseAll);

            Handles.EndGUI();
        }

#endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Resources

        static private Mesh CreateVolatileMesh(string name) {
            Mesh m = new Mesh();
            m.name = name;
            m.hideFlags = HideFlags.DontSave;
            m.MarkDynamic();
            return m;
        }

        private void EnsureGUIResources() {
            if (!m_InitializedResources) {
                m_TextStylePlain = new GUIStyle(GUIStyle.none);
                m_TextStylePlain.font = m_TextFont;
                m_TextStylePlain.alignment = TextAnchor.MiddleCenter;
                m_TextStylePlain.clipping = TextClipping.Overflow;
                m_TextStylePlain.fontStyle = FontStyle.Normal;
                m_TextStylePlain.fontSize = 0;
                m_TextStylePlain.normal.textColor = Color.white;

                m_TextStyleBox = new GUIStyle(m_TextStylePlain);
                m_TextStyleBox.normal.background = Texture2D.whiteTexture;
                m_TextStyleBox.padding = new RectOffset(8, 8, 4, 4);

                m_TextContent = new GUIContent();
                m_InitializedResources = true;
            }
        }

        #endregion // Resources

        #region Rendering

        private void RenderLines(float deltaTime, Vector3 invCameraLook, BitSet64 mask, bool allowRendering) {
            for (int i = s_ActiveLines.Count - 1; i >= 0; i--) {
                ref Vector3x2RenderState state = ref s_ActiveLines[i];

                if (allowRendering && (state.Params.Category < 0 || mask.IsSet(state.Params.Category))) {
                    MeshData16<DebugVertexFormat> meshData;
                    if (state.Params.DepthTest) {
                        meshData = m_MainMeshData;
                    } else {
                        meshData = m_OverlayMeshData;
                    }

                    Vector3 vector = (state.Max - state.Min).normalized;
                    Vector3 perpendicular = Vector3.Cross(invCameraLook, vector).normalized;
                    perpendicular *= (0.5f * m_LineWidthToWorldScale * state.Params.LineWidth);

                    DebugVertexFormat a, b, c, d;
                    a.Color = b.Color = c.Color = d.Color = state.Params.Color;
                    a.Position = state.Min - perpendicular;
                    b.Position = state.Max - perpendicular;
                    c.Position = state.Min + perpendicular;
                    d.Position = state.Max + perpendicular;
                    meshData.AddQuad(a, b, c, d);
                }

                if (deltaTime > 0) {
                    state.State.Duration -= deltaTime;
                    if (state.State.Duration <= 0) {
                        s_ActiveLines.FastRemoveAt(i);
                    }
                }
            }
        }

        private void RenderSpheres(float deltaTime, BitSet64 mask, bool allowRendering) {
            for (int i = s_ActiveSpheres.Count - 1; i >= 0; i--) {
                ref SphereRenderState state = ref s_ActiveSpheres[i];

                if (allowRendering && (state.Params.Category < 0 || mask.IsSet(state.Params.Category))) {
                    float scale = state.Radius / m_SphereMeshDefaultRadius;
                    Matrix4x4 pos = Matrix4x4.TRS(state.Center, Quaternion.identity, new Vector3(scale, scale, scale));

                    RenderParams renderParams;
                    if (state.Params.DepthTest) {
                        renderParams = new RenderParams(m_DepthTestMaterial);
                    } else {
                        renderParams = new RenderParams(m_OverlayMaterial);
                    }

                    m_TempMaterialPropertyBlock.SetColor("_Color", state.Params.Color);
                    renderParams.matProps = m_TempMaterialPropertyBlock;

                    Graphics.RenderMesh(renderParams, m_SphereMesh, 0, pos);
                }

                if (deltaTime > 0) {
                    state.State.Duration -= deltaTime;
                    if (state.State.Duration <= 0) {
                        s_ActiveSpheres.FastRemoveAt(i);
                    }
                }
            }
        }

        private void RenderText(float deltaTime, Camera camera, BitSet64 mask, bool allowRendering) {
            int screenW = Screen.width, screenH = Screen.height;
            for (int i = s_ActiveTexts.Count - 1; i >= 0; i--) {
                ref TextRenderState state = ref s_ActiveTexts[i];

                if (allowRendering && (state.Params.Category < 0 || mask.IsSet(state.Params.Category))) {
                    Vector2 targetPoint;

                    if (state.WorldSpace) {
                        targetPoint = camera.WorldToScreenPoint(state.Position);
                    } else {
                        targetPoint = new Vector2(state.Position.x * screenW, state.Position.y * screenH);
                    }

                    targetPoint.y = screenH - targetPoint.y;
                    targetPoint.x += state.Offset.x;
                    targetPoint.y -= state.Offset.y;

                    GUIStyle style;
                    switch (state.Style) {
                        case DebugTextStyle.BackgroundDark: {
                            style = m_TextStyleBox;
                            GUI.backgroundColor = Color.black.WithAlpha(0.7f);
                            break;
                        }
                        case DebugTextStyle.BackgroundDarkOpaque: {
                            style = m_TextStyleBox;
                            GUI.backgroundColor = Color.black;
                            break;
                        }
                        case DebugTextStyle.BackgroundLight: {
                            style = m_TextStyleBox;
                            GUI.backgroundColor = Color.white.WithAlpha(0.7f);
                            break;
                        }
                        case DebugTextStyle.BackgroundLightOpaque: {
                            style = m_TextStyleBox;
                            GUI.backgroundColor = Color.white;
                            break;
                        }
                        default: {
                            style = m_TextStylePlain;
                            break;
                        }
                    }

                    style.alignment = state.Alignment;
                    m_TextContent.text = state.Text;

                    Vector2 size = style.CalcSize(m_TextContent);

                    switch (state.Alignment) {
                        case TextAnchor.UpperCenter:
                        case TextAnchor.MiddleCenter:
                        case TextAnchor.LowerCenter: {
                            targetPoint.x -= size.x / 2;
                            break;
                        }

                        case TextAnchor.UpperRight:
                        case TextAnchor.MiddleRight:
                        case TextAnchor.LowerRight: {
                            targetPoint.x -= size.x;
                            break;
                        }
                    }

                    switch (state.Alignment) {
                        case TextAnchor.MiddleLeft:
                        case TextAnchor.MiddleCenter:
                        case TextAnchor.MiddleRight: {
                            targetPoint.y -= size.y / 2;
                            break;
                        }

                        case TextAnchor.LowerLeft:
                        case TextAnchor.LowerCenter:
                        case TextAnchor.LowerRight: {
                            targetPoint.y -= size.y;
                            break;
                        }
                    }

                    GUI.contentColor = state.Params.Color;
                    GUI.Label(new Rect((int) targetPoint.x, (int) targetPoint.y, (int) size.x, (int) size.y), m_TextContent, style);
                }

                if (deltaTime > 0) {
                    state.State.Duration -= deltaTime;
                    if (state.State.Duration <= 0) {
                        s_ActiveTexts.FastRemoveAt(i);
                    }
                }
            }
        }

        private void DecayText(float deltaTime) {
            for (int i = s_ActiveTexts.Count - 1; i >= 0; i--) {
                ref TextRenderState state = ref s_ActiveTexts[i];

                if (deltaTime > 0) {
                    state.State.Duration -= deltaTime;
                    if (state.State.Duration <= 0) {
                        s_ActiveTexts.FastRemoveAt(i);
                    }
                }
            }
        }

        #endregion // Rendering

#endif // DEVELOPMENT

        #region Static API

        /// <summary>
        /// Adds text, pinned to a world-space point, to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddWorldText(Vector3 point, string text, Color color, float duration = 0, TextAnchor alignment = TextAnchor.MiddleCenter, DebugTextStyle style = DebugTextStyle.Default, int category = -1) {
#if DEVELOPMENT && !SKIP_ONGUI
            TextRenderState renderState = new TextRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = false;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.WorldSpace = true;
            renderState.Text = text;
            renderState.Position = point;
            renderState.Alignment = alignment;
            renderState.Style = style;
            s_ActiveTexts.PushBack(renderState);
#endif // DEVELOPMENT && !SKIP_ONGUI
        }

        /// <summary>
        /// Adds text, pinned to a world-space point, to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddWorldText(Vector3 point, Vector2 offset, string text, Color color, float duration = 0, TextAnchor alignment = TextAnchor.MiddleCenter, DebugTextStyle style = DebugTextStyle.Default, int category = -1) {
#if DEVELOPMENT && !SKIP_ONGUI
            TextRenderState renderState = new TextRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = false;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.WorldSpace = true;
            renderState.Text = text;
            renderState.Position = point;
            renderState.Offset = offset;
            renderState.Alignment = alignment;
            renderState.Style = style;
            s_ActiveTexts.PushBack(renderState);
#endif // DEVELOPMENT && !SKIP_ONGUI
        }

        /// <summary>
        /// Adds text, pinned to a viewport point, to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddViewportText(Vector2 viewport, string text, Color color, float duration = 0, TextAnchor alignment = TextAnchor.MiddleCenter, DebugTextStyle style = DebugTextStyle.Default, int category = -1) {
#if DEVELOPMENT && !SKIP_ONGUI
            TextRenderState renderState = new TextRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = false;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.WorldSpace = false;
            renderState.Text = text;
            renderState.Position = viewport;
            renderState.Alignment = alignment;
            renderState.Style = style;
            s_ActiveTexts.PushBack(renderState);
#endif // DEVELOPMENT && !SKIP_ONGUI
        }

        /// <summary>
        /// Adds text, pinned to a viewport point, to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddViewportText(Vector2 viewport, Vector2 offset, string text, Color color, float duration = 0, TextAnchor alignment = TextAnchor.MiddleCenter, DebugTextStyle style = DebugTextStyle.Default, int category = -1) {
#if DEVELOPMENT && !SKIP_ONGUI
            TextRenderState renderState = new TextRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = false;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.WorldSpace = false;
            renderState.Text = text;
            renderState.Position = viewport;
            renderState.Offset = offset;
            renderState.Alignment = alignment;
            renderState.Style = style;
            s_ActiveTexts.PushBack(renderState);
#endif // DEVELOPMENT && !SKIP_ONGUI
        }

        /// <summary>
        /// Adds an AABB to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddBounds(Bounds bounds, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            AddBounds(bounds.min, bounds.max, color, lineWidth, duration, depthTest, category);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds an AABB to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddBounds(Vector3 pointMin, Vector3 pointMax, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            unsafe {
                Vector3* corners = stackalloc Vector3[8];
                Vector3 min = pointMin;
                Vector3 max = pointMax;
                corners[0] = min;
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                corners[4] = new Vector3(max.x, min.y, min.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(max.x, max.y, min.z);
                corners[7] = max;

                SubmitBox(corners, color, lineWidth, duration, depthTest, category);
            }
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds an OOBB to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddOrientedBounds(Matrix4x4 center, Bounds bounds, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            unsafe {
                Vector3* corners = stackalloc Vector3[8];
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                corners[0] = min;
                corners[1] = new Vector3(min.x, min.y, max.z);
                corners[2] = new Vector3(min.x, max.y, min.z);
                corners[3] = new Vector3(min.x, max.y, max.z);
                corners[4] = new Vector3(max.x, min.y, min.z);
                corners[5] = new Vector3(max.x, min.y, max.z);
                corners[6] = new Vector3(max.x, max.y, min.z);
                corners[7] = max;

                for (int i = 0; i < 8; i++) {
                    corners[i] = center.MultiplyPoint3x4(corners[i]);
                }

                SubmitBox(corners, color, lineWidth, duration, depthTest, category);
            }
#endif // DEVELOPMENT
        }

        static private unsafe void SubmitBox(Vector3* corners, Color color, float lineWidth, float duration, bool depthTest, int category = -1) {
            AddLine(corners[0], corners[1], color, lineWidth, duration, depthTest, category);
            AddLine(corners[0], corners[2], color, lineWidth, duration, depthTest, category);
            AddLine(corners[0], corners[4], color, lineWidth, duration, depthTest, category);

            AddLine(corners[1], corners[3], color, lineWidth, duration, depthTest, category);
            AddLine(corners[1], corners[5], color, lineWidth, duration, depthTest, category);

            AddLine(corners[2], corners[3], color, lineWidth, duration, depthTest, category);
            AddLine(corners[2], corners[6], color, lineWidth, duration, depthTest, category);

            AddLine(corners[3], corners[7], color, lineWidth, duration, depthTest, category);

            AddLine(corners[4], corners[5], color, lineWidth, duration, depthTest, category);
            AddLine(corners[4], corners[6], color, lineWidth, duration, depthTest, category);

            AddLine(corners[5], corners[7], color, lineWidth, duration, depthTest, category);

            AddLine(corners[6], corners[7], color, lineWidth, duration, depthTest, category);
        }

        /// <summary>
        /// Adds a line to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddLine(Vector3 start, Vector3 end, Color color, float lineWidth = 1, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            Vector3x2RenderState renderState = new Vector3x2RenderState();
            renderState.Params.Color = color;
            renderState.Params.LineWidth = lineWidth;
            renderState.Params.DepthTest = depthTest;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.Min = start;
            renderState.Max = end;
            s_ActiveLines.PushBack(renderState);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds a sphere to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddSphere(Vector3 center, float radius, Color color, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            SphereRenderState renderState = new SphereRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = depthTest;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.Center = center;
            renderState.Radius = radius;
            renderState.Solid = false;
            s_ActiveSpheres.PushBack(renderState);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds a point to the debug render queue.
        /// </summary>
        [Conditional("DEVELOPMENT"), Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        static public void AddPoint(Vector3 center, float size, Color color, float duration = 0, bool depthTest = true, int category = -1) {
#if DEVELOPMENT
            SphereRenderState renderState = new SphereRenderState();
            renderState.Params.Color = color;
            renderState.Params.DepthTest = depthTest;
            renderState.Params.Category = (sbyte) category;
            renderState.State.Duration = duration;
            renderState.Center = center;
            renderState.Radius = size;
            renderState.Solid = true;
            s_ActiveSpheres.PushBack(renderState);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Enables the given debug drawing category.
        /// Debug primitives with a set category will only render if that category is enabled.
        /// </summary>
        static public void EnableCategory(int category) {
#if DEVELOPMENT
            s_CategoryMask.Set(category);
            Log.Msg("[DebugDraw] Category {0} enabled", category);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Disables the given debug drawing category.
        /// Debug primitives with a set category will only render if that category is enabled.
        /// </summary>
        static public void DisableCategory(int category) {
#if DEVELOPMENT
            s_CategoryMask.Unset(category);
            Log.Msg("[DebugDraw] Category {0} disabled", category);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Returns if the given object is selected for debug draw.
        /// </summary>
        static public bool IsSelected(UnityEngine.Object obj) {
#if DEVELOPMENT && UNITY_EDITOR
            return Selection.Contains(obj);
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Enables debug rendering.
        /// </summary>
        static public void EnableRendering() {
#if DEVELOPMENT
            s_PauseAll = false;
            Log.Msg("[DebugDraw] Rendering enabled");
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Disables debug rendering.
        /// </summary>
        static public void DisableRendering() {
#if DEVELOPMENT
            s_PauseAll = true;
            Log.Msg("[DebugDraw] Rendering disabled");
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Returns if debug rendering is enabled.
        /// </summary>
        static public bool IsRenderingEnabled() {
#if DEVELOPMENT
            return !s_PauseAll;
#else
            return false;
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds a toggle for the given category to a debug menu.
        /// </summary>
        static public void AddCategoryToggle(DMInfo info, int category, string name, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
            info.AddToggle(name, () => s_CategoryMask.IsSet(category), (b) => {
                if (b) {
                    EnableCategory(category);
                } else {
                    DisableCategory(category);
                }
            }, predicate, indent);
#endif // DEVELOPMENT
        }

        /// <summary>
        /// Adds a toggle for all debug rendering to a debug menu.
        /// </summary>
        static public void AddRenderToggle(DMInfo info, string name, DMPredicate predicate = null, int indent = 0) {
#if DEVELOPMENT
            info.AddToggle(name ?? "Enable Debug Rendering", () => !s_PauseAll, (b) => {
                if (b) {
                    EnableRendering();
                } else {
                    DisableRendering();
                }
            }, predicate, indent);
#endif // DEVELOPMENT
        }

        #endregion // Static API

        [EngineMenuFactory]
        static private DMInfo CreateDrawingEngineMenu() {
            DMInfo info = new DMInfo("Debug Drawing", 4);
            AddRenderToggle(info, null);
            return info;
        }
    }

    /// <summary>
    /// Text display style.
    /// </summary>
    public enum DebugTextStyle {
        /// <summary>
        /// No background.
        /// </summary>
        Default,

        /// <summary>
        /// Transparent black background.
        /// </summary>
        BackgroundDark,

        /// <summary>
        /// Opaque black background.
        /// </summary>
        BackgroundDarkOpaque,

        /// <summary>
        /// Transparent white background.
        /// </summary>
        BackgroundLight,

        /// <summary>
        /// Opaque white background.
        /// </summary>
        BackgroundLightOpaque
    }
}