#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USE_URP
#endif // UNITY_2019_1_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;
using UnityEngine.Rendering;

#if USE_URP
using UnityEngine.Rendering.Universal;
#endif // USE_URP

namespace FieldDay.Rendering {
    /// <summary>
    /// Camera utility functions.
    /// </summary>
    static public class CameraUtility {
        static private readonly Camera[] s_CameraWorkArray = new Camera[256];

        /// <summary>
        /// Finds the most specific camera that renders the given layer.
        /// </summary>
        static public Camera FindMostSpecificCameraForLayer(int layer, bool includeInactive = true) {
            int cameraCount = Camera.GetAllCameras(s_CameraWorkArray);

            Camera found = null;
            int mostSpecificBitCount = int.MaxValue;

            layer = (1 << layer);

            for (int i = 0; i < cameraCount; ++i) {
                Camera cam = s_CameraWorkArray[i];
                if (!includeInactive && !cam.isActiveAndEnabled)
                    continue;

                int camCullingMask = cam.cullingMask;

                if ((camCullingMask & layer) == layer) {
                    int bitCount = Bits.Count(camCullingMask);
                    if (bitCount < mostSpecificBitCount) {
                        found = cam;
                        mostSpecificBitCount = bitCount;
                    }
                }
            }

            Array.Clear(s_CameraWorkArray, 0, cameraCount);
            return found;
        }

        /// <summary>
        /// Returns if any cameras are set to render directly to the screen/backbuffer.
        /// </summary>
        static public bool AreAnyCamerasDirectlyRendering() {
            return AreAnyCamerasDirectlyRendering(null);
        }

        /// <summary>
        /// Returns if any cameras are set to render directly to the screen/backbuffer.
        /// </summary>
        static public bool AreAnyCamerasDirectlyRendering(Camera excludeCamera) {
            int cameraCount = Camera.GetAllCameras(s_CameraWorkArray);
            bool found = false;

            for(int i = 0; i < cameraCount; i++) {
                Camera c = s_CameraWorkArray[i];
                if (!ReferenceEquals(c, excludeCamera) && c.isActiveAndEnabled && WillRenderDirectly(c)) {
                    found = true;
                    break;
                }
            }

            Array.Clear(s_CameraWorkArray, 0, cameraCount);
            return found;
        }

        /// <summary>
        /// Returns if the given camera will render directly to the screen/backbuffer.
        /// </summary>
        static public bool WillRenderDirectly(Camera camera) {
            // cameras rendering to a target
            if (camera.targetTexture != null) {
                return false;
            }

#if USE_URP
            // overlay cameras
            var data = camera.GetUniversalAdditionalCameraData();
            if (data.renderType == CameraRenderType.Overlay) {
                return false;
            }
#endif // USE_URP

            return true;
        }

        /// <summary>
        /// Returns if the given camera is a game camera.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsGameCamera(Camera camera) {
            return camera.cameraType == CameraType.Game;
        }
    
        /// <summary>
        /// Gets a matrix that faces towards the given camera.
        /// </summary>
        static public void GetBillboardingMatrix(Camera camera, out Matrix4x4 matrix) {
            Transform cameraTransform = camera.transform;
            matrix = Matrix4x4.Rotate(Quaternion.LookRotation(-Geom.Forward(cameraTransform.rotation), Vector3.up));
        }

        /// <summary>
        /// Gets a matrix that faces towards the given camera.
        /// </summary>
        static public void GetBillboardingMatrix(Camera camera, Vector3 upVector, out Matrix4x4 matrix) {
            Transform cameraTransform = camera.transform;
            matrix = Matrix4x4.Rotate(Quaternion.LookRotation(-Geom.Forward(cameraTransform.rotation), upVector));
        }

        /// <summary>
        /// Returns the size of the camera frustum at a given distance.
        /// </summary>
        static public Vector2 GetFrustumSize(Camera camera, float z) {
            Vector2 size;
            if (camera.orthographic) {
                size.y = camera.orthographicSize * 2;
            } else {
                size.y = CameraHelper.HeightForDistanceAndFOV(z, camera.fieldOfView);
            }
            size.x = size.y * camera.aspect;
            return size;
        }

        /// <summary>
        /// Renders the given camera to the given RenderTexture.
        /// </summary>
        static public void RenderToTexture(Camera camera, RenderTexture texture) {
            Game.Rendering.PushManualRender();

            Rect prevRect = camera.rect;
            RenderTexture prevRT = camera.targetTexture;
            
            camera.rect = new Rect(0, 0, 1, 1);
            camera.targetTexture = texture;

            camera.Render();

            camera.targetTexture = prevRT;
            camera.rect = prevRect;

            Game.Rendering.PopManualRender();
        }

        /// <summary>
        /// Renders the given camera to a screenshot.
        /// </summary>
        static public Texture2D RenderToScreenshot(Camera camera, CameraScreenshotFlags flags, float renderScale = 1) {
            Assert.True(renderScale >= 1);

            Game.Rendering.PushManualRender();

            Rect prevRect = camera.rect;
            RenderTexture prevRT = camera.targetTexture;
            RenderTexture prevTarget = RenderTexture.active;
            CameraRenderScale rsComponent = camera.GetComponent<CameraRenderScale>();

            float rsScale = 1;
            CameraRenderScale.ScaleMode rsMode = default;
            if ((flags & CameraScreenshotFlags.OverrideRenderScaleComponent) != 0 && rsComponent) {
                rsScale = rsComponent.Scale;
                rsMode = rsComponent.Mode;

                rsScale = 1;
                rsMode = CameraRenderScale.ScaleMode.Scale;
            }

            RenderTextureDescriptor descriptor = new RenderTextureDescriptor((int) (camera.pixelWidth * renderScale), (int) (camera.pixelHeight * renderScale), RenderTextureFormat.Default, 32);
            descriptor.autoGenerateMips = false;
            RenderTexture tempRT = RenderTexture.GetTemporary(descriptor);
            tempRT.antiAliasing = 1;
            tempRT.filterMode = FilterMode.Bilinear;

            Texture2D screenshotTex = new Texture2D(tempRT.width, tempRT.height, TextureFormat.RGB24, false, true);

            camera.rect = new Rect(0, 0, 1, 1);
            camera.targetTexture = tempRT;

            using (Profiling.Time("rendering screenshot")) {
                camera.Render();
            }

            camera.targetTexture = prevRT;
            camera.rect = prevRect;

            if ((flags & CameraScreenshotFlags.OverrideRenderScaleComponent) != 0 && rsComponent) {
                rsComponent.Scale = rsScale;
                rsComponent.Mode = rsMode;
            }

            using (Profiling.Time("reading screenshot pixels")) {
                RenderTexture.active = tempRT;
                screenshotTex.ReadPixels(new Rect(0, 0, screenshotTex.width, screenshotTex.height), 0, 0, false);
            }

            if (QualitySettings.activeColorSpace == ColorSpace.Linear) {
                using (Profiling.Time("converting to gamma color space")) {
                    Color[] pixels = screenshotTex.GetPixels();
                    for (int i = 0; i < pixels.Length; i++) {
                        pixels[i] = pixels[i].gamma;
                    }
                    screenshotTex.SetPixels(pixels);
                }
            }

            RenderTexture.active = prevTarget;
            RenderTexture.ReleaseTemporary(tempRT);

            Game.Rendering.PopManualRender();

            return screenshotTex;
        }
    }

    [Flags]
    public enum CameraScreenshotFlags {
        OverrideRenderScaleComponent = 0x01
    }
}