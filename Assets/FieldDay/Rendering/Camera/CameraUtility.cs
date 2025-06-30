#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USE_URP
#endif // UNITY_2019_1_OR_NEWER

using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;

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
    }
}