using TMPro;
using UnityEngine;
using System;
using System.Collections;

using FieldDay;
using FieldDay.UI;
using FieldDay.Vox;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using Astro.Audio;
using BeauUtil.UI;

namespace Astro {
    public class SubtitleDisplay : SharedRoutinePanel, IRegistrationCallbacks, IOnGuiUpdate {
        #region Inspector

        [Header("Display")]
        [SerializeField] private TMP_Text m_Text;
        [SerializeField] private RoundedRectGraphic m_Background;

        [Header("Data")]
        [SerializeField] private ColorPalette2 m_DefaultColors;

        #endregion // Inspector

        [NonSerialized] private TMP_FontAsset m_DefaultFont;
        [NonSerialized] private float m_DefaultFontSize;
        [NonSerialized] private float m_DefaultCornerRadius;
        [NonSerialized] private Vector4 m_DefaultMargin;

        [NonSerialized] private SubtitleDisplayData m_CurrentDisplayData;
        [NonSerialized] private VoxWaveform m_CurrentWaveform;
        [NonSerialized] private bool m_UpdateRegistered;

        private Routine m_BounceAnim;

        #region IRegistrationCallbacks

        void IRegistrationCallbacks.OnRegister() {
            m_DefaultFont = m_Text.font;
            m_DefaultFontSize = m_Text.fontSize;
            m_DefaultCornerRadius = m_Background.CornerRadius;
            m_DefaultMargin = m_Text.margin;

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
            m_CurrentWaveform = default;
            Hide(0.5f);
        }

        #endregion // Handlers

        private void SyncDisplayedData(SubtitleDisplayData data) {
            m_Text.SetText(data.Subtitle.Data);

            SubtitleStyle style;

            Game.Assets.TryGetNamed(data.CharacterId, out style);

            ColorPalette2 palette = m_DefaultColors;
            TMP_FontAsset font = m_DefaultFont;
            float fontSize = m_DefaultFontSize;
            float cornerRadius = m_DefaultCornerRadius;
            Vector4 margin = m_DefaultMargin;

            if (style != null) {
                if (style.OverrideColors) {
                    palette = style.Colors;
                }
                if (style.OverrideFont) {
                    font = style.OverrideFont;
                }

                fontSize *= style.FontScale;
                cornerRadius *= style.BackgroundCornerRadiusScale;
                margin *= style.MarginScale;
            }

            m_Text.color = palette.Content;
            m_Background.SetColor(palette.Background);
            m_Text.font = font;
            m_Text.fontSize = fontSize;
            m_Background.CornerRadius = cornerRadius;
            m_Text.margin = margin;

            VoxWaveformTable table = Find.NamedAsset<VoxWaveformTable>("VoxTable");
            table.TryFind(VoxUtility.GetLineCode(data.VoxHandle), out m_CurrentWaveform);
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

        protected override void OnShow(bool inbInstant) {
            if (!m_UpdateRegistered) {
                Game.Gui.RegisterUpdate(this);
                m_UpdateRegistered = true;
            }
        }

        protected override void OnHideComplete(bool inbInstant) {
            m_Text.SetText(string.Empty);
            if (m_UpdateRegistered) {
                Game.Gui?.DeregisterUpdate(this);
                m_UpdateRegistered = false;
            }
        }

        void IOnGuiUpdate.OnGuiUpdate() {
            //if (m_CurrentWaveform.Chunks.Length > 0 && VoxUtility.IsPlaying(m_CurrentDisplayData.VoxHandle)) {
            //    float time = VoxUtility.GetPlaybackPosition(m_CurrentDisplayData.VoxHandle);
            //    float duration = VoxUtility.GetDuration(m_CurrentDisplayData.VoxHandle);
            //    float amp = VoxWaveform.ReadAmplitude(m_CurrentWaveform, time, duration);
            //    Debug.Log(amp);
            //    m_Text.rectTransform.SetScale(1 + amp, Axis.Y);
            //}
        }

        #endregion // Animation
    }
}