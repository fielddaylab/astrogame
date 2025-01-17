using TMPro;
using UnityEngine;
using System;
using System.Collections;

using FieldDay;
using FieldDay.UI;
using FieldDay.Vox;
using BeauRoutine;

namespace Astro {
    public class SubtitleDisplay : SharedRoutinePanel, IRegistrationCallbacks {
        #region Inspector

        [Header("Display")]
        [SerializeField] private TMP_Text m_Text;

        #endregion // Inspector

        [NonSerialized] private SubtitleDisplayData m_CurrentDisplayData;
        private Routine m_BounceAnim;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            SubtitleUtility.OnDisplayRequested.Register(HandleDisplayRequest);
            SubtitleUtility.OnDismissRequested.Register(HandleDismissRequest);
        }

        void IRegistrationCallbacks.OnDeregister() {
            SubtitleUtility.OnDisplayRequested.Deregister(HandleDisplayRequest);
            SubtitleUtility.OnDismissRequested.Deregister(HandleDismissRequest);
        }

        #endregion IRegistrationCallbacks

        #region Handlers

        private void HandleDisplayRequest(SubtitleDisplayData data) {
            if (data.Priority < m_CurrentDisplayData.Priority || string.IsNullOrEmpty(data.Subtitle.Data)) {
                return;
            }

            m_CurrentDisplayData = data;
            SyncDisplayedData(data);

            if (IsShowing()) {
                m_BounceAnim.Replace(this, BounceAnim());
            } else {
                Show();
            }
        }

        private void HandleDismissRequest(SubtitleDisplayData data) {
            if (data.VoxHandle != m_CurrentDisplayData.VoxHandle) {
                return;
            }

            m_CurrentDisplayData = default;
            Hide(0.5f);
        }

        #endregion // Handlers

        private void SyncDisplayedData(SubtitleDisplayData data) {
            m_Text.SetText(data.Subtitle.Data);
        }

        #region Animation

        protected override void InstantTransitionToHide() {
            Root.gameObject.SetActive(false);
            CanvasGroup.alpha = 0;
            m_LayoutOffset.Offset0 = default;
        }

        protected override void InstantTransitionToShow() {
            Root.gameObject.SetActive(true);
            CanvasGroup.alpha = 1;
            SyncDisplayedData(m_CurrentDisplayData);
            m_LayoutOffset.Offset0 = default;
        }

        protected override IEnumerator TransitionToHide() {
            m_LayoutOffset.Offset0 = default;
            yield return CanvasGroup.FadeTo(0, 0.25f).Ease(Curve.QuadIn);
            Root.gameObject.SetActive(false);
        }

        protected override IEnumerator TransitionToShow() {
            if (!Root.gameObject.activeSelf) {
                Root.gameObject.SetActive(true);
                CanvasGroup.alpha = 0;
                m_LayoutOffset.Offset0 = new Vector2(0, -4);
                yield return Routine.Combine(
                    CanvasGroup.FadeTo(1, 0.25f).Ease(Curve.CubeIn),
                    m_LayoutOffset.Offset0To(new Vector2(0, 0), 0.25f).Ease(Curve.CubeIn)
                    );
            } else {
                CanvasGroup.alpha = 1;
                m_BounceAnim.Replace(this, BounceAnim());
            }
        }

        private IEnumerator BounceAnim() {
            m_LayoutOffset.Offset0 = new Vector2(0, -4);
            return m_LayoutOffset.Offset0To(new Vector2(0, 0), 0.25f).Ease(Curve.BackOut);
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_Text.SetText(string.Empty);
        }

        #endregion // Animation
    }
}