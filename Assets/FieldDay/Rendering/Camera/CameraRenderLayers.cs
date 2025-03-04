using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [AddComponentMenu("Field Day/Cameras/Camera Render Layers")]
    public class CameraRenderLayers : MonoBehaviour {
        [NonSerialized] private Camera m_Camera;

        [NonSerialized] private RingBuffer<LayerMask> m_MaskStack = new RingBuffer<LayerMask>(2, RingBufferMode.Expand);
        [NonSerialized] private LayerMask m_QueuedMask;
        [NonSerialized] private int m_HideDepth;

        public Camera Camera {
            get { return this.CacheComponent(ref m_Camera); }
        }

        private void Awake() {
            this.CacheComponent(ref m_Camera);
            m_HideDepth = !m_Camera.enabled || m_Camera.cullingMask == 0 ? 1 : 0;
            m_QueuedMask = m_Camera.cullingMask;
        }

        #region Masks

        public LayerMask Mask {
            get { return m_HideDepth > 0 ? m_QueuedMask : m_Camera.cullingMask; }
            set {
                if (m_HideDepth > 0) {
                    m_QueuedMask = value;
                } else {
                    m_Camera.cullingMask = value;
                }
            }
        }

        public void ShowLayers(LayerMask mask) {
            if (m_HideDepth > 0) {
                m_QueuedMask.value |= mask.value;
            } else {
                m_Camera.cullingMask |= mask.value;
            }
        }

        public void HideLayers(LayerMask mask) {
            if (m_HideDepth > 0) {
                m_QueuedMask.value &= ~mask.value;
            } else {
                m_Camera.cullingMask &= ~mask.value;
            }
        }

        public bool IsRenderingLayersAny(LayerMask mask) {
            if (m_HideDepth > 0) {
                return (m_QueuedMask.value & mask.value) != 0;
            } else {
                return (m_Camera.cullingMask & mask.value) != 0;
            }
        }

        #endregion // Masks

        #region All

        public void ShowAll() {
            if (m_HideDepth > 0) {
                m_HideDepth--;
                if (m_HideDepth == 0) {
                    m_Camera.cullingMask = m_QueuedMask;
                }
            }
        }

        public void HideAll() {
            m_HideDepth++;
            if (m_HideDepth == 1) {
                m_QueuedMask = m_Camera.cullingMask;
                m_Camera.cullingMask = 0;
            }
        }

        #endregion // All

        #region Stack

        public void PushMask() {
            m_MaskStack.PushBack(Mask);
        }

        public void PopMask() {
            if (m_MaskStack.TryPopBack(out LayerMask mask)) {
                Mask = mask;
            }
        }

        #endregion // Stack
    }
}