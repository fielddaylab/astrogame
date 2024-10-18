using System;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Rendering {
    /// <summary>
    /// Sets the given camera to clamp to the virtual viewport.
    /// </summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-10000)]
    public sealed class CameraClampToVirtualViewport : MonoBehaviour {
        public Rect Viewport = new Rect(0, 0, 1, 1);
        [NonSerialized] private Camera m_Camera;

        public Camera Camera {
            get { return this.CacheComponent(ref m_Camera); }
        }

        private void OnEnable() {
            this.CacheComponent(ref m_Camera);
            Game.Rendering.AddClampedViewportCamera(this);
        }

        private void OnDisable() {
            Game.Rendering?.RemoveClampedViewportCamera(this);
        }
    }
}