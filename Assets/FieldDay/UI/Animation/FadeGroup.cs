using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Animation {
    [AddComponentMenu("Field Day/Canvas/Components/Fade Group")]
    public sealed class FadeGroup : MonoBehaviour, IGuiPanel {
        public CanvasGroup Group;
        public LayoutOffset Offset;

        [Header("Timing")]
        public TweenSettings ToOnTween = new TweenSettings(0.2f);
        public TweenSettings ToOffTween = new TweenSettings(0.2f);

        [Header("Offsets")]
        public Vector2 ToOnStartOffset;
        public Vector2 DefaultOffset;
        public Vector2 ToOffEndOffset;

        [Header("Raycast Adjustments")]
        public bool SyncBlocksRaycasts;

        [NonSerialized] public Transform CachedTransform;
        [NonSerialized] public AnimHandle CurrentHandle;
        [NonSerialized] public bool CurrentState;

        private void Awake() {
            gameObject.SetActive(false);
            CurrentState = isActiveAndEnabled;
        }

        #region IGuiPanel

        public Transform Root { get { return this.CacheComponent(ref CachedTransform); } }

        public void Show() {
            if (!CurrentState) {
                CurrentState = true;
                Game.Animation.CancelAnimation(ref CurrentHandle);
                CurrentHandle = Game.Animation.AddLiteAnimator(FadeInAnimator, this, 0);
            }
        }

        public void Hide() {
            if (CurrentState) {
                CurrentState = false;
                Game.Animation.CancelAnimation(ref CurrentHandle);
                CurrentHandle = Game.Animation.AddLiteAnimator(FadeOutAnimator, this, 0);
            }
        }

        public bool IsShowing() {
            return CurrentState;
        }

        public bool IsTransitioning() {
            return Game.Animation.IsAnimationRunning(CurrentHandle);
        }

        public bool IsVisible() {
            return CurrentState && Group.alpha > 0;
        }

        public void SetVisibleNow(bool visible) {
            Game.Animation.CancelAnimation(ref CurrentHandle);
            CurrentState = visible;

            if (Offset) {
                Offset.Offset2 = visible ? DefaultOffset : ToOnStartOffset;
            }
            Group.alpha = visible ? 1 : 0;
            Group.gameObject.SetActive(visible);

            if (SyncBlocksRaycasts) {
                Group.blocksRaycasts = visible;
            }
        }

        #endregion // IGuiPanel

        #region Anims

        private sealed class FadeInAnim : LiteAnimator<FadeGroup> {
            public override void InitAnimation(FadeGroup target, ref LiteAnimatorState state) {
                if (!target.Group.gameObject.activeSelf) {
                    target.Group.alpha = 0;
                    if (target.SyncBlocksRaycasts) {
                        target.Group.blocksRaycasts = false;
                    }
                    GuiCommands.SetActive(target.Group.gameObject, true);
                }

                target.CurrentState = true;
                state.InitParamA.Float = target.Group.alpha;
                state.ResetTime(target.ToOnTween.Time * (1 - state.InitParamA.Float));
            }

            public override void ResetAnimation(FadeGroup target, ref LiteAnimatorState state) {
            }

            public override bool UpdateAnimation(FadeGroup target, ref LiteAnimatorState state, float deltaTime) {
                state.TimeRemaining -= deltaTime;
                float percent = 1 - Math.Max(0, state.TimeRemaining / state.Duration);

                target.Group.alpha = Mathf.LerpUnclamped(state.InitParamA.Float, 1, percent);
                if (state.TimeRemaining > 0) {
                    return true;
                } else {
                    if (target.SyncBlocksRaycasts) {
                        target.Group.blocksRaycasts = true;
                    }
                    return false;
                }
            }
        }

        private sealed class FadeOutAnim : LiteAnimator<FadeGroup> {
            public override void InitAnimation(FadeGroup target, ref LiteAnimatorState state) {
                target.CurrentState = false;
                state.InitParamA.Float = target.Group.alpha;
                state.ResetTime(target.ToOffTween.Time * (state.InitParamA.Float));
                if (target.SyncBlocksRaycasts) {
                    target.Group.blocksRaycasts = false;
                }
            }

            public override void ResetAnimation(FadeGroup target, ref LiteAnimatorState state) {
            }

            public override bool UpdateAnimation(FadeGroup target, ref LiteAnimatorState state, float deltaTime) {
                state.TimeRemaining -= deltaTime;
                float percent = 1 - Math.Max(0, state.TimeRemaining / state.Duration);

                target.Group.alpha = Mathf.LerpUnclamped(state.InitParamA.Float, 0, percent);
                if (state.TimeRemaining > 0) {
                    return true;
                } else {
                    GuiCommands.SetActive(target.Group.gameObject, false);
                    return false;
                }
            }
        }

        static private readonly FadeInAnim FadeInAnimator = new FadeInAnim();
        static private readonly FadeOutAnim FadeOutAnimator = new FadeOutAnim();

        // TODO: bounce anims

        #endregion // Anims
    }
}