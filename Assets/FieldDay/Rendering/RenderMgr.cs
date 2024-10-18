#if UNITY_2019_1_OR_NEWER
#define USE_SRP
#endif // UNITY_2019_1_OR_NEWER

#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USING_URP
#endif // UNITY_2019_1_OR_NEWER

using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Debugging;
using UnityEngine;

#if USE_SRP
using UnityEngine.Rendering;
#endif // USE_SRP

#if USING_URP
using UnityEngine.Rendering.Universal;
#endif // USING_URP

namespace FieldDay.Rendering {
    public sealed class RenderMgr : ICameraPreRenderCallback, ICameraPreCullCallback, ICameraPostRenderCallback {
#if DEVELOPMENT

        private struct CameraRestoreData {
            public int CameraId;

            public bool PostProcessing;
            public float FOV;
            public float Ortho;
            public int CullingMask;

#if USING_URP
            public AntialiasingMode AA;
            public AntialiasingQuality AAQuality;
#endif // USING_URP

            public void Apply(Camera camera) {
                camera.cullingMask = CullingMask;
                camera.orthographicSize = Ortho;
                camera.fieldOfView = FOV;

#if USING_URP
                var data = camera.GetUniversalAdditionalCameraData();
                if (data) {
                    data.renderPostProcessing = PostProcessing;

                    data.antialiasing = AA;
                    data.antialiasingQuality = AAQuality;
                }
#endif // USING_URP
            }

            public void CreateFrom(Camera camera) {
                CameraId = camera.GetInstanceID();

                CullingMask = camera.cullingMask;
                Ortho = camera.orthographicSize;
                FOV = camera.fieldOfView;

#if USING_URP
                var data = camera.GetUniversalAdditionalCameraData();
                if (data) {
                    PostProcessing = data.renderPostProcessing;

                    AA = data.antialiasing;
                    AAQuality = data.antialiasingQuality;
                }
#endif // USING_URP
            }
        }

        private struct DebugCameraAdjustments {
            public bool DisablePostProcessing;
            public int DisableLayers;
            public int ForceLayers;
            public float? AdjustFOV;
            public float? AdjustOrthoSize;

#if USING_URP
            public AntialiasingMode? AA;
            public AntialiasingQuality? AAQuality;
#endif // USING_URP

            public bool CachedActive;

            public bool CheckIsActive() {
#if USING_URP
                if (AA.HasValue || AAQuality.HasValue) {
                    return true;
                }
#endif // USING_URP
                return DisablePostProcessing || DisableLayers != 0 || ForceLayers != 0
                    || AdjustFOV.HasValue || AdjustOrthoSize.HasValue;
            }

            public void Apply(Camera camera) {
                camera.cullingMask = (camera.cullingMask | ForceLayers) & ~DisableLayers;
                if (AdjustFOV.HasValue) {
                    camera.fieldOfView = AdjustFOV.Value;
                }
                if (AdjustOrthoSize.HasValue) {
                    camera.orthographicSize = AdjustOrthoSize.Value;
                }

#if USING_URP
                var data = camera.GetUniversalAdditionalCameraData();
                if (DisablePostProcessing) {
                    data.renderPostProcessing = false;
                }

                if (AA.HasValue) {
                    data.antialiasing = AA.Value;
                }
                if (AAQuality.HasValue) {
                    data.antialiasingQuality = AAQuality.Value;
                }
#endif // USING_URP
            }
        }

#endif // DEVELOPMENT

        private bool m_LastKnownFullscreen;
        private Resolution m_LastKnownResolution;

        private Camera m_PrimaryCamera;
        private Camera m_FallbackCamera;

        private RingBuffer<CameraClampToVirtualViewport> m_ClampedViewportCameras = new RingBuffer<CameraClampToVirtualViewport>(2, RingBufferMode.Expand);
        private Rect m_VirtualViewport = new Rect(0, 0, 1, 1);

        private float m_MinAspect;
        private float m_MaxAspect;
        private bool m_HasLetterboxing;

        private bool m_ShouldCheckFallback = true;
        private bool m_UsingFallback = false;
        private ushort m_LastLetterboxFrameRendered = Frame.InvalidIndex;

#if DEVELOPMENT

        private CameraRestoreData m_DebugPrimaryCameraRestore;
        private DebugCameraAdjustments m_DebugPrimaryCameraAdjustments;

        private void CacheDebugCameraAdjustments() {
            m_DebugPrimaryCameraAdjustments.CachedActive = m_DebugPrimaryCameraAdjustments.CheckIsActive();
        }

#endif // DEVELOPMENT

        #region Callbacks

        public readonly CastableEvent<bool> OnFullscreenChanged = new CastableEvent<bool>(2);
        public readonly CastableEvent<Resolution> OnResolutionChanged = new CastableEvent<Resolution>(2);

        #endregion // Callbacks

        #region Events

        internal void Initialize() {
            GameLoop.OnCanvasPreRender.Register(OnCanvasPreUpdate);
            GameLoop.OnApplicationPreRender.Register(OnApplicationPreRender);
            GameLoop.OnFrameAdvance.Register(OnApplicationPostRender);

            Game.Scenes.OnAnySceneUnloaded.Register(OnSceneLoadUnload);
            Game.Scenes.OnAnySceneEnabled.Register(OnSceneLoadUnload);
            Game.Scenes.OnSceneReady.Register(OnSceneLoadUnload);

            CameraHelper.AddOnPreCull(this);
            CameraHelper.AddOnPreRender(this);
            CameraHelper.AddOnPostRender(this);
        }

        internal void LateInitialize() {
            Game.Gui.OnPrimaryCameraChanged.Register(OnGuiCameraChanged);
            OnGuiCameraChanged(Game.Gui.PrimaryCamera);
        }

        internal void PollScreenSettings() {
            bool fullscreen = ScreenUtility.GetFullscreen();
            if (m_LastKnownFullscreen != fullscreen) {
                m_LastKnownFullscreen = fullscreen;
                OnFullscreenChanged.Invoke(fullscreen);
            }

            Resolution resolution = ScreenUtility.GetResolution();
            if (resolution.width != m_LastKnownResolution.width || resolution.height != m_LastKnownResolution.height
#if UNITY_2022_2_OR_NEWER
                || !resolution.refreshRateRatio.Equals(m_LastKnownResolution.refreshRateRatio)
#else
                || resolution.refreshRate != m_LastKnownResolution.refreshRate
#endif // UNITY_2022_2_OR_NEWER
                ) {
                m_LastKnownResolution = resolution;
                OnResolutionChanged.Invoke(resolution);
            }
        }

        internal void Shutdown() {
            GameLoop.OnCanvasPreRender.Deregister(OnCanvasPreUpdate);
            GameLoop.OnApplicationPreRender.Deregister(OnApplicationPreRender);
            GameLoop.OnFrameAdvance.Deregister(OnApplicationPostRender);

            Game.Scenes.OnAnySceneUnloaded.Deregister(OnSceneLoadUnload);
            Game.Scenes.OnAnySceneEnabled.Deregister(OnSceneLoadUnload);
            Game.Scenes.OnSceneReady.Deregister(OnSceneLoadUnload);

            CameraHelper.RemoveOnPreCull(this);
            CameraHelper.RemoveOnPreRender(this);
            CameraHelper.RemoveOnPostRender(this);

            OnResolutionChanged.Clear();
            OnFullscreenChanged.Clear();
        }

        #endregion // Events

        #region World Camera

        public Camera PrimaryCamera {
            get { return m_PrimaryCamera; }
        }

        public void SetPrimaryCamera(Camera camera) {
            if (m_PrimaryCamera != null) {
                Log.Warn("[RenderMgr] Primary world camera already set to '{0}' - make sure to deregister it first", m_PrimaryCamera);
            }
            m_PrimaryCamera = camera;
            m_ShouldCheckFallback = true;
            Log.Msg("[RenderMgr] Assigned primary world camera as '{0}'", camera);
        }

        public void RemovePrimaryCamera(Camera camera) {
            if (camera == null || m_PrimaryCamera != camera) {
                return;
            }

            m_PrimaryCamera = null;
            m_ShouldCheckFallback = true;
            Log.Msg("[RenderMgr] Removed primary world camera");
        }

        #endregion // World Camera

        #region Clamped Viewport

        public void EnableAspectClamping(int width, int height) {
            m_MinAspect = (float) width / height;
            m_MaxAspect = m_MinAspect;
        }

        public void EnableMinimumAspectClamping(int width, int height) {
            m_MinAspect = (float) width / height;
            m_MaxAspect = float.MaxValue;
        }

        public void EnableAspectClamping(Vector2Int min, Vector2Int max) {
            m_MinAspect = (float) min.x / min.y;
            m_MaxAspect = (float) max.x / max.y;
        }

        public void DisableAspectClamping() {
            m_MinAspect = m_MaxAspect = 0;
            m_VirtualViewport = new Rect(0, 0, 1, 1);
        }

        public Rect VirtualViewport {
            get { return m_VirtualViewport; }
        }

        public void AddClampedViewportCamera(CameraClampToVirtualViewport camera) {
            Assert.NotNull(camera);
            m_ClampedViewportCameras.PushBack(camera);
        }

        public void RemoveClampedViewportCamera(CameraClampToVirtualViewport camera) {
            Assert.NotNull(camera);
            m_ClampedViewportCameras.FastRemove(camera);
        }

        #endregion // Clamped Viewport

        #region Fallback

        public bool HasFallbackCamera() {
            return m_FallbackCamera;
        }

        public void CreateDefaultFallbackCamera() {
            if (m_FallbackCamera) {
                Log.Warn("[RenderMgr] Fallback camera already in place.");
                return;
            }

            GameObject go = new GameObject("[RenderMgr Fallback]");
            Camera camera = go.AddComponent<Camera>();
            GameObject.DontDestroyOnLoad(go);
            go.SetActive(false);

            camera.cullingMask = 0;
            camera.orthographic = true;
            camera.orthographicSize = 0.5f;
            camera.backgroundColor = Color.black;
            camera.clearFlags = CameraClearFlags.SolidColor | CameraClearFlags.Depth;
            camera.depth = -100;

#if USING_URP
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            data.renderShadows = false;
            data.renderPostProcessing = false;
            data.requiresDepthTexture = false;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresColorTexture = false;
            data.stopNaN = false;
            data.dithering = false;
#endif // USING_URP

            m_FallbackCamera = camera;

            Log.Msg("[RenderMgr] Created default fallback camera");

            OnGuiCameraChanged(Game.Gui.PrimaryCamera);
            go.SetActive(m_UsingFallback);
        }

        // TODO: SetCustomFallbackCamera

        /// <summary>
        /// Marks the "fallback camera" state as dirty.
        /// This will force it to be reevaluated before the next render.
        /// </summary>
        public void QueueFallbackCameraReevaluate() {
            m_ShouldCheckFallback = true;
        }

        #endregion // Fallback

        #region Handlers

        private void OnGuiCameraChanged(Camera uiCam) {
            if (!m_FallbackCamera) {
                return;
            }
#if USING_URP
            var data = m_FallbackCamera.GetUniversalAdditionalCameraData();
            if (data) {
                if (uiCam != null) {
                    if (!data.cameraStack.Contains(uiCam)) {
                        data.cameraStack.Add(uiCam);
                    }
                } else {
                    data.cameraStack.Clear();
                }
            }
#endif // USING_URP
        }

        private void OnSceneLoadUnload() {
            m_ShouldCheckFallback = true;
        }

        private void CheckIfNeedsFallback() {
            if (!m_ShouldCheckFallback) {
                return;
            }

            bool needsFallback = !CameraUtility.AreAnyCamerasDirectlyRendering(m_FallbackCamera);
            if (Ref.Replace(ref m_UsingFallback, needsFallback)) {
                if (m_FallbackCamera) {
                    m_FallbackCamera.gameObject.SetActive(needsFallback);
                }
                Log.Msg("[RenderMgr] Fallback camera switched to {0}", needsFallback ? "ON" : "OFF");
            }
            
            m_ShouldCheckFallback = false;
        }

        private void OnCanvasPreUpdate() {
#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Canvas pre-update");
            }
#endif // DEVELOPMENT

            if (m_MinAspect <= 0 || m_MaxAspect <= 0) {
                m_HasLetterboxing = false;
                return;
            }

            m_VirtualViewport = UpdateAspectRatioClamping(m_LastKnownResolution.width, m_LastKnownResolution.height, m_MinAspect, m_MaxAspect);
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Virtual viewport is {0}", m_VirtualViewport.ToString());
            }

            for(int i = 0; i < m_ClampedViewportCameras.Count; i++) {
                ref var c = ref m_ClampedViewportCameras[i];
                Rect r = c.Viewport;
                r.x = m_VirtualViewport.x + r.x * m_VirtualViewport.width;
                r.y = m_VirtualViewport.y + r.y * m_VirtualViewport.height;
                r.width = r.width * m_VirtualViewport.width;
                r.height = r.height * m_VirtualViewport.height;
                c.Camera.rect = r;
            }

            m_HasLetterboxing = true;
        }

        private void OnApplicationPreRender() {
#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Application pre-render");
            }
#endif // DEVELOPMENT

            CheckIfNeedsFallback();
        }

        private void OnApplicationPostRender() {
#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Application post-render");
            }
#endif // DEVELOPMENT
        }

        static private Rect UpdateAspectRatioClamping(float w, float h, float min, float max) {
            float currentAspect = (float) w / h;
            float finalAspect = Mathf.Clamp(currentAspect, min, max);

            float aspectW = finalAspect;
            float aspectH = 1;

            if (aspectW > currentAspect) {
                aspectH = currentAspect / finalAspect;
                aspectW = aspectH * finalAspect;
            }

            float diffX = 1 - (aspectW / currentAspect),
                diffY = 1 - (aspectH / 1);

            Rect r = default;
            r.x = diffX / 2;
            r.y = diffY / 2;
            r.width = 1 - diffX;
            r.height = 1 - diffY;

            return r;
        }

        #endregion // Handlers

        #region Camera Callbacks

        void ICameraPreCullCallback.OnCameraPreCull(Camera inCamera, CameraCallbackSource inSource) {
            if (!GameLoop.IsRenderingOrPreparingRendering() || !CameraUtility.IsGameCamera(inCamera)) {
                return;
            }

#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Camera '{0}' pre-cull", inCamera.name);
            }
#endif // DEVELOPMENT

            AttemptRenderLetterboxing();

#if DEVELOPMENT
            if (m_DebugPrimaryCameraRestore.CameraId == 0 && m_DebugPrimaryCameraAdjustments.CachedActive && ReferenceEquals(inCamera, m_PrimaryCamera)) {
                if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                    Log.Trace("[RenderMgr] Applying debug changes to primary camera");
                }
                m_DebugPrimaryCameraRestore.CreateFrom(inCamera);
                m_DebugPrimaryCameraAdjustments.Apply(inCamera);
            }
#endif // DEVELOPMENT
        }

        void ICameraPreRenderCallback.OnCameraPreRender(Camera inCamera, CameraCallbackSource inSource) {
            if (!GameLoop.IsRendering() || !CameraUtility.IsGameCamera(inCamera)) {
                return;
            }

#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Camera '{0}' pre-render", inCamera.name);
            }
#endif // DEVELOPMENT

#if DEVELOPMENT
            if (m_DebugPrimaryCameraRestore.CameraId == 0 && m_DebugPrimaryCameraAdjustments.CachedActive && ReferenceEquals(inCamera, m_PrimaryCamera)) {
                if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                    Log.Trace("[RenderMgr] Applying debug changes to primary camera");
                }
                m_DebugPrimaryCameraRestore.CreateFrom(inCamera);
                m_DebugPrimaryCameraAdjustments.Apply(inCamera);
            }
#endif // DEVELOPMENT
        }

        private void AttemptRenderLetterboxing() {

            if (m_LastLetterboxFrameRendered != Frame.Index) {
                m_LastLetterboxFrameRendered = Frame.Index;

                RenderTexture prevRenderTarget = null;
                bool switchedRenderTargets = false;

                if (m_UsingFallback && !m_FallbackCamera) {
                    if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                        Log.Trace("[RenderMgr] Clearing backbuffer as fallback");
                    }

                    if (!switchedRenderTargets) {
                        switchedRenderTargets = true;
                        prevRenderTarget = RenderTexture.active;
                        Graphics.SetRenderTarget(null);
                    }

                    GL.PushMatrix();
                    GL.LoadOrtho();
                    GL.Clear(true, true, Color.black, 1);
                    GL.PopMatrix();
                }

                if (m_HasLetterboxing && m_ClampedViewportCameras.Count > 0) {
                    if (!switchedRenderTargets) {
                        switchedRenderTargets = true;
                        prevRenderTarget = RenderTexture.active;
                        Graphics.SetRenderTarget(null);
                    }

                    GL.Viewport(new Rect(0, 0, m_LastKnownResolution.width, m_LastKnownResolution.height));
                    if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                        Log.Trace("[RenderMgr] Rendering letterboxing for viewport {0}", m_VirtualViewport.ToString());
                    }
                    CameraHelper.RenderLetterboxing(m_VirtualViewport, Color.black);
                }

                if (DebugFlags.IsFlagSet(DebuggingFlags.VisualizeEntireScreen)) {
                    if (!switchedRenderTargets) {
                        switchedRenderTargets = true;
                        prevRenderTarget = RenderTexture.active;
                        Graphics.SetRenderTarget(null);
                    }

                    GL.PushMatrix();
                    GL.LoadOrtho();
                    GL.Viewport(new Rect(0, 0, m_LastKnownResolution.width, m_LastKnownResolution.height));
                    GL.Clear(true, true, Color.magenta, 1);
                    GL.PopMatrix();

                    string debugText = string.Format("Screen Dimensions: {0} ({1})", m_LastKnownResolution, m_LastKnownFullscreen ? "FULLSCREEN" : "NOT FULLSCREEN");

                    DebugDraw.AddViewportText(new Vector2(0.5f, 1), new Vector2(0, -8), debugText, Color.white, 0, TextAnchor.UpperCenter, DebugTextStyle.BackgroundDarkOpaque);
                }

                if (switchedRenderTargets) {
                    Graphics.SetRenderTarget(prevRenderTarget);
                }
            }
        }

        void ICameraPostRenderCallback.OnCameraPostRender(Camera inCamera, CameraCallbackSource inSource) {
            if (!GameLoop.IsRendering() || !CameraUtility.IsGameCamera(inCamera)) {
                return;
            }

#if DEVELOPMENT
            if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                Log.Trace("[RenderMgr] Camera '{0}' post-render", inCamera.name);
            }
#endif // DEVELOPMENT

#if DEVELOPMENT

            if (m_DebugPrimaryCameraRestore.CameraId == inCamera.GetInstanceID()) {
                m_DebugPrimaryCameraRestore.Apply(inCamera);
                m_DebugPrimaryCameraRestore = default;
                if (DebugFlags.IsFlagSet(DebuggingFlags.TraceExecution)) {
                    Log.Trace("[RenderMgr] Undoing debug changes to primary camera");
                }
            }

#endif // DEVELOPMENT
        }

        #endregion // Camera Callbacks

        #region Debug

        private enum DebuggingFlags {
            TraceExecution,
            VisualizeEntireScreen
        }

#if DEVELOPMENT

        [EngineMenuFactory]
        static private DMInfo CreateRenderDebugMenu() {
            DMInfo info = new DMInfo("Rendering", 16);
            DebugFlags.Menu.AddSingleFrameFlagButton(info, "Trace Execution for Frame", DebuggingFlags.TraceExecution);
            DebugFlags.Menu.AddFlagToggle(info, "Render Debug Info", DebuggingFlags.VisualizeEntireScreen);
            info.AddDivider();

            DMInfo postProcessingMenu = new DMInfo("Post Processing", 4);
            postProcessingMenu.AddToggle("Suppress Post-Processing", () => Game.Rendering.m_DebugPrimaryCameraAdjustments.DisablePostProcessing, (b) => {
                Game.Rendering.m_DebugPrimaryCameraAdjustments.DisablePostProcessing = b;
                Game.Rendering.CacheDebugCameraAdjustments();
            });

            info.AddSubmenu(postProcessingMenu);

            DMInfo renderLayerMenu = new DMInfo("Rendering Layers");
            renderLayerMenu.MinimumWidth = 250;

            for (int i = 0; i < 32; i++) {
                string layerName = LayerMask.LayerToName(i);

                if (string.IsNullOrEmpty(layerName)) {
                    continue;
                }

                int idx = i;
                renderLayerMenu.AddSlider(layerName, () => {
                    if (Bits.Contains(Game.Rendering.m_DebugPrimaryCameraAdjustments.ForceLayers, idx)) {
                        return 2;
                    } else if (Bits.Contains(Game.Rendering.m_DebugPrimaryCameraAdjustments.DisableLayers, idx)) {
                        return 1;
                    } else {
                        return 0;
                    }
                }, (f) => {
                    Bits.Set(ref Game.Rendering.m_DebugPrimaryCameraAdjustments.ForceLayers, idx, f == 2);
                    Bits.Set(ref Game.Rendering.m_DebugPrimaryCameraAdjustments.DisableLayers, idx, f == 1);
                    Game.Rendering.CacheDebugCameraAdjustments();
                }, 0, 2, 1, (f) => {
                    if (f == 0) {
                        return "(Scene Default)";
                    } else if (f == 1) {
                        return "Disabled";
                    } else {
                        return "Always";
                    }
                });
            }

            info.AddSubmenu(renderLayerMenu);

            DMInfo antialiasingSettings = new DMInfo("Antialiasing");
            antialiasingSettings.MinimumWidth = 250;
#if USING_URP
            var renderPipeline = UniversalRenderPipeline.asset;
            antialiasingSettings.AddSlider("Antialiasing", () => {
                var aa = Game.Rendering.m_DebugPrimaryCameraAdjustments.AA;
                if (aa.HasValue) {
                    return 1 + (int) aa.Value;
                } else {
                    return 0;
                }
            }, (f) => {
                int m = (int) f;
                AntialiasingMode? mode = m == 0 ? null : (AntialiasingMode) (m - 1);
                Game.Rendering.m_DebugPrimaryCameraAdjustments.AA = mode;
                Game.Rendering.CacheDebugCameraAdjustments();
            }, 0, 3, 1, (f) => {
                int m = (int) f;
                if (m == 0) {
                    return "(Scene Default)";
                } else {
                    return ((AntialiasingMode) (m - 1)).ToString();
                }
            });

            antialiasingSettings.AddSlider("Quality", () => {
                var quality = Game.Rendering.m_DebugPrimaryCameraAdjustments.AAQuality;
                if (quality.HasValue) {
                    return 1 + (int) quality.Value;
                } else {
                    return 0;
                }
            }, (f) => {
                int q = (int) f;
                AntialiasingQuality? quality = q == 0 ? null : (AntialiasingQuality) (q - 1);
                Game.Rendering.m_DebugPrimaryCameraAdjustments.AAQuality = quality;
                Game.Rendering.CacheDebugCameraAdjustments();
            }, 0, 3, 1, (f) => {
                int m = (int) f;
                if (m == 0) {
                    return "(Scene Default)";
                } else {
                    return ((AntialiasingQuality) (m - 1)).ToString();
                }
            }, null, 1);
#else

#endif // USING_URP

            info.AddSubmenu(antialiasingSettings);

            DMInfo qualitySettings = new DMInfo("Quality Settings");

            info.AddSubmenu(qualitySettings);

            return info;
        }

#endif // DEVELOPMENT

        #endregion // Debug
    }
}