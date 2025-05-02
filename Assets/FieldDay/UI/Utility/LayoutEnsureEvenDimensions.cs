using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine.Scripting;

namespace UnityEngine.UI {
    /// <summary>
    /// Offsets sizeDelta to ensure width and height are always evenly divisible by 2.
    /// </summary>
    [RequireComponent(typeof(RectTransform)), DisallowMultipleComponent, DefaultExecutionOrder(10000)]
    public sealed class LayoutEnsureEvenDimensions : MonoBehaviour, ILayoutElement, ILayoutSelfController {
        [NonSerialized] private RectTransform m_Rect;
        [NonSerialized] private Canvas m_Canvas;
        [NonSerialized] private Vector2 m_Applied;
        [NonSerialized] private Vector2 m_LastCanvasSize;

        #region Events

        private void OnEnable() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
                m_Canvas = m_Rect.GetCanvas();
            }
            ApplyCurrentOffset();
        }

        private void OnDisable() {
            if (m_Rect) {
                ApplyOffset(default(Vector2));
            }
            m_LastCanvasSize = default;
        }

        private void LateUpdate() {
            ApplyCurrentOffset();
        }

        private void OnTransformParentChanged() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
            m_Canvas = m_Rect.GetCanvas();
        }

        #endregion // Events

        private void ApplyCurrentOffset() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
                m_Canvas = m_Rect.GetCanvas();
            }

            Vector2 canvasSize = m_Canvas.pixelRect.size;
            if (m_LastCanvasSize != canvasSize) {
                m_LastCanvasSize = canvasSize;
                Vector2 quantizedRectSize = canvasSize;
                quantizedRectSize.x = ((int) quantizedRectSize.x / 2) * 2;
                quantizedRectSize.y = ((int) quantizedRectSize.y / 2) * 2;
                Vector2 offset = quantizedRectSize - canvasSize;
                ApplyOffset(offset);
            }
        }

        private void ApplyOffset(Vector2 offset) {
            Vector2 delta = offset - m_Applied;
            m_Applied = offset;

            if (delta.x != 0 || delta.y != 0) {
                if (object.ReferenceEquals(m_Rect, null)) {
                    m_Rect = (RectTransform) transform;
                }
                m_Rect.offsetMax += delta;
            }
        }

        #region ILayout

        float ILayoutElement.minWidth { get { return -1; } }
        float ILayoutElement.preferredWidth { get { return -1; } }
        float ILayoutElement.flexibleWidth { get { return -1; } }

        float ILayoutElement.minHeight { get { return -1; } }
        float ILayoutElement.preferredHeight { get { return -1; } }
        float ILayoutElement.flexibleHeight { get { return -1; } }

        int ILayoutElement.layoutPriority { get { return -10001; } }

        void ILayoutElement.CalculateLayoutInputHorizontal() {
#if UNITY_EDITOR
            if (!Application.IsPlaying(this))
                return;
#endif // UNITY_EDITOR
            ApplyOffset(default(Vector2));
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
            // Ignore
        }

        void ILayoutController.SetLayoutHorizontal() {
            // Ignore
        }

        void ILayoutController.SetLayoutVertical() {
#if UNITY_EDITOR
            if (!Application.IsPlaying(this))
                return;
#endif // UNITY_EDITOR
            ApplyCurrentOffset();
        }

        #endregion // ILayout
    }
}