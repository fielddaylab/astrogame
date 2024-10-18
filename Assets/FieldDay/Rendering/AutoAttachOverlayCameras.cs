#if UNITY_2019_1_OR_NEWER && HAS_URP
#define USE_URP
#endif // UNITY_2019_1_OR_NEWER

using BeauUtil;
using UnityEngine;
using System;
using BeauUtil.Debugger;

#if USE_URP
using UnityEngine.Rendering.Universal;
#endif // USE_URP

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif // UNITY_EDITOR

namespace FieldDay.Rendering {
    /// <summary>
    /// Automatically attaches overlay cameras to this camera.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera)), ExecuteAlways]
    [AddComponentMenu("Field Day/Cameras/Auto Attach Overlays")]
    public sealed class AutoAttachOverlayCameras : MonoBehaviour {
        [SerializeField, UnityTag] private string[] m_Tags = Array.Empty<string>();
        [NonSerialized] private readonly RingBuffer<Camera> m_CachedAddedCameras = new RingBuffer<Camera>();

        private void OnEnable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;

            if (!Application.IsPlaying(this) && (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)) {
                return;
            }
#endif // UNITY_EDITOR

#if USE_URP
            Camera c = GetComponent<Camera>();
            var data = c.GetUniversalAdditionalCameraData();
            var stack = data.cameraStack;

            foreach(var tag in m_Tags) {
                GameObject foundGO = GameObject.FindGameObjectWithTag(tag);
                if (foundGO != null && foundGO.TryGetComponent(out Camera cam) && cam.GetUniversalAdditionalCameraData().renderType == CameraRenderType.Overlay) {
                    if (!stack.Contains(cam)) {
                        stack.Add(cam);
                    }
                    m_CachedAddedCameras.PushBack(cam);
                }
            }
#else
            Log.Warn("[AutoAttachOverlayCameras] URP not detected - overlay cameras cannot be attached");
#endif // USE_URP
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                return;
#endif // UNITY_EDITOR

#if USE_URP
            Camera c = GetComponent<Camera>();
            var data = c.GetUniversalAdditionalCameraData();
            var stack = data.cameraStack;

            while (m_CachedAddedCameras.TryPopBack(out var overlay)) {
                stack.Remove(overlay);
            }
#endif // USE_URP
        }

        public void OverrideTags(string[] tags) {
            if (isActiveAndEnabled) {
                enabled = false;
                m_Tags = tags ?? Array.Empty<string>();
                enabled = true;
            } else {
                m_Tags = tags ?? Array.Empty<string>();
            }
        }
    }
}