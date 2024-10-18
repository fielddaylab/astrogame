using System;
using BeauRoutine;
using UnityEngine.Scripting;

namespace UnityEngine.UI {
    /// <summary>
    /// Offset for a RectTransform's anchoredPosition.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(RectTransform)), DisallowMultipleComponent]
    public sealed class LayoutOffset : MonoBehaviour, ILayoutElement, ILayoutSelfController {
        #region Inspector

        [SerializeField] private Vector2 m_Offset0;
        [SerializeField] private Vector2 m_Offset1;
        [SerializeField] private Vector2 m_Offset2;

        #endregion // Inspector

        [NonSerialized] private RectTransform m_Rect;
        [NonSerialized] private Vector2 m_Applied;

        #region Properties

        public Vector2 Offset0 {
            get { return m_Offset0; }
            set {
                if (m_Offset0 != value) {
                    m_Offset0 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        public Vector2 Offset1 {
            get { return m_Offset1; }
            set {
                if (m_Offset1 != value) {
                    m_Offset1 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        public Vector2 Offset2 {
            get { return m_Offset2; }
            set {
                if (m_Offset2 != value) {
                    m_Offset2 = value;
                    ApplyCurrentOffset();
                }
            }
        }

        #endregion // Properties

        #region Events

        private void OnEnable() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
            ApplyCurrentOffset();
        }

        private void OnDisable() {
            if (m_Rect) {
                ApplyOffset(default(Vector2));
            }
        }

        private void OnTransformParentChanged() {
            if (object.ReferenceEquals(m_Rect, null)) {
                m_Rect = (RectTransform) transform;
            }
        }

        [Preserve]
        private void OnDidApplyAnimationProperties() {
            ApplyCurrentOffset();
        }

#if UNITY_EDITOR

        private void OnValidate() {
            if (!isActiveAndEnabled) {
                return;
            }

            if (!Application.IsPlaying(this) && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) {
                return;
            }

            ApplyCurrentOffset();
        }

#endif // UNITY_EDITOR

        #endregion // Events

        private void ApplyCurrentOffset() {
            ApplyOffset(m_Offset0 + m_Offset1 + m_Offset2);
        }

        private void ApplyOffset(Vector2 offset) {
            Vector2 delta = offset - m_Applied;
            m_Applied = offset;

            if (delta.x != 0 || delta.y != 0) {
                if (object.ReferenceEquals(m_Rect, null)) {
                    m_Rect = (RectTransform) transform;
                }
                m_Rect.anchoredPosition += delta;
            }
        }

        #region ILayout

        float ILayoutElement.minWidth { get { return -1; } }
        float ILayoutElement.preferredWidth { get { return -1; } }
        float ILayoutElement.flexibleWidth { get { return -1; } }

        float ILayoutElement.minHeight { get { return -1; } }
        float ILayoutElement.preferredHeight { get { return -1; } }
        float ILayoutElement.flexibleHeight { get { return -1; } }

        int ILayoutElement.layoutPriority { get { return -10000; } }

        void ILayoutElement.CalculateLayoutInputHorizontal() {
            ApplyOffset(default(Vector2));
        }

        void ILayoutElement.CalculateLayoutInputVertical() {
            // Ignore
        }

        void ILayoutController.SetLayoutHorizontal() {
            // Ignore
        }

        void ILayoutController.SetLayoutVertical() {
            ApplyCurrentOffset();
        }

        #endregion // ILayout

        #region Tweens

        private class Offset0Tween : ITweenData {
            private LayoutOffset m_Offset;
            private Vector2 m_Target;
            private Vector2 m_Start;
            private Vector2 m_Delta;

            public Offset0Tween(LayoutOffset offset, Vector2 target) {
                m_Offset = offset;
                m_Target = target;
            }

            public void ApplyTween(float inPercent) {
                m_Offset.Offset0 = m_Start + m_Delta * inPercent;
            }

            public void OnTweenEnd() {
            }

            public void OnTweenStart() {
                m_Start = m_Offset.m_Offset0;
                m_Delta = m_Target - m_Start;
            }
        }

        private class Offset1Tween : ITweenData {
            private LayoutOffset m_Offset;
            private Vector2 m_Target;
            private Vector2 m_Start;
            private Vector2 m_Delta;

            public Offset1Tween(LayoutOffset offset, Vector2 target) {
                m_Offset = offset;
                m_Target = target;
            }

            public void ApplyTween(float inPercent) {
                m_Offset.Offset1 = m_Start + m_Delta * inPercent;
            }

            public void OnTweenEnd() {
            }

            public void OnTweenStart() {
                m_Start = m_Offset.m_Offset1;
                m_Delta = m_Target - m_Start;
            }
        }

        private class Offset2Tween : ITweenData {
            private LayoutOffset m_Offset;
            private Vector2 m_Target;
            private Vector2 m_Start;
            private Vector2 m_Delta;

            public Offset2Tween(LayoutOffset offset, Vector2 target) {
                m_Offset = offset;
                m_Target = target;
            }

            public void ApplyTween(float inPercent) {
                m_Offset.Offset2 = m_Start + m_Delta * inPercent;
            }

            public void OnTweenEnd() {
            }

            public void OnTweenStart() {
                m_Start = m_Offset.m_Offset2;
                m_Delta = m_Target - m_Start;
            }
        }

        public Tween Offset0To(Vector2 offset, float duration) {
            return Tween.Create(new Offset0Tween(this, offset), duration);
        }

        public Tween Offset1To(Vector2 offset, float duration) {
            return Tween.Create(new Offset1Tween(this, offset), duration);
        }

        public Tween Offset2To(Vector2 offset, float duration) {
            return Tween.Create(new Offset2Tween(this, offset), duration);
        }

        #endregion // Tweens
    }
}