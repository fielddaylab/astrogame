using BeauUtil;
using FieldDay.Animation;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace FieldDay.UI.Animation {
    public sealed class LoadingIcon : BaseGuiModule {
        #region Inspector

        [SerializeField] private Canvas m_CanvasLayer;

        [Header("Components")]
        [SerializeField] private CanvasGroup m_FadeGroup;

        [Header("Timing")]
        [SerializeField] private float m_DefaultFadeInDelay = 1;
        [SerializeField] private float m_FadeInDuration = 0.3f;
        [SerializeField] private float m_FadeOutDuration = 0.3f;

        #endregion // Inspector

        [NonSerialized] private bool m_Visible;
        [NonSerialized] private float m_Progress;
        [NonSerialized] private AnimHandle m_FadeAnim;

        public CastableEvent<LoadingIcon> BeginAnimation = new CastableEvent<LoadingIcon>();
        public CastableEvent<LoadingIcon> EndAnimation = new CastableEvent<LoadingIcon>();
        public CastableEvent<LoadingIcon, float> UpdateProgress = new CastableEvent<LoadingIcon, float>();

        protected override void Awake() {
            base.Awake();

            m_CanvasLayer.enabled = false;
            m_FadeGroup.alpha = 0;
        }

        public void Show() {
            Show(m_DefaultFadeInDelay);
        }

        public void Show(float delay) {
            if (m_Visible) {
                return;
            }

            m_Visible = true;
            m_Progress = 0;
            Game.Animation.CancelAnimation(ref m_FadeAnim);

            LiteAnimatorState state = default;
            state.ResetTimeWithDelay(m_FadeInDuration, delay);
            m_FadeAnim = Game.Animation.AddLiteAnimator(s_FadeInInstance, this, state, GameLoopPhase.UnscaledUpdate);
        }
        
        public void Hide() {
            if (!m_Visible) {
                return;
            }

            m_Visible = false;
            Game.Animation.CancelAnimation(ref m_FadeAnim);

            LiteAnimatorState state = default;
            state.ResetTime(m_FadeOutDuration);
            m_FadeAnim = Game.Animation.AddLiteAnimator(s_FadeOutInstance, this, state, GameLoopPhase.UnscaledUpdate);
        }

        private sealed class FadeInAnim : LiteAnimator<LoadingIcon> {
            public override void InitAnimation(LoadingIcon target, ref LiteAnimatorState state) {
            }

            public override void ResetAnimation(LoadingIcon target, ref LiteAnimatorState state) {
            }

            public override bool UpdateAnimation(LoadingIcon target, ref LiteAnimatorState state, float deltaTime) {
                state.TimeRemaining -= deltaTime;
                float percent = 1 - Math.Max(0, state.TimeRemaining / state.Duration);

                bool bWasZero = target.m_FadeGroup.alpha <= 0;
                target.m_FadeGroup.alpha = percent;

                if (bWasZero && percent > 0) {
                    target.m_CanvasLayer.enabled = true;
                    target.BeginAnimation.Invoke(target);
                }

                return state.TimeRemaining > 0;
            }
        }

        private sealed class FadeOutAnim : LiteAnimator<LoadingIcon> {
            public override void InitAnimation(LoadingIcon target, ref LiteAnimatorState state) {
            }

            public override void ResetAnimation(LoadingIcon target, ref LiteAnimatorState state) {
            }

            public override bool UpdateAnimation(LoadingIcon target, ref LiteAnimatorState state, float deltaTime) {
                state.TimeRemaining -= deltaTime;
                float percent = 1 - Math.Max(0, state.TimeRemaining / state.Duration);

                target.m_FadeGroup.alpha = 1 - percent;

                if (percent >= 1) {
                    target.m_CanvasLayer.enabled = false;
                    target.EndAnimation.Invoke(target);
                }

                return state.TimeRemaining > 0;
            }
        }

        static private readonly FadeInAnim s_FadeInInstance = new FadeInAnim();
        static private readonly FadeOutAnim s_FadeOutInstance = new FadeOutAnim();
    }
}