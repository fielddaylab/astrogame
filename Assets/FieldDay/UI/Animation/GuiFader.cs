using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.UI;
using FieldDay.Animation;
using UnityEngine;
using UnityEngine.UI;

namespace FieldDay.UI.Animation {
    [AddComponentMenu("Field Day/Canvas/Components/Fader")]
    public sealed class GuiFader : MonoBehaviour, IPooledObject<GuiFader> {
        [Required] public Graphic Graphic;

        [NonSerialized] public AnimHandle SequenceHandle;
        [NonSerialized] public AnimHandle FadeHandle;

        [NonSerialized] public IPool<GuiFader> SourcePool;
        [NonSerialized] private bool m_Allocated;

        public AnimHandle Show(Color target, float duration) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            return StartShow(target, duration, Curve.Linear);
        }

        public AnimHandle Hide(float duration, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            return StartHide(duration, 0, Curve.Linear, autoFree);
        }

        public AnimHandle Hide(float duration, float delay, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            return StartHide(duration, delay, Curve.Linear, autoFree);
        }

        public AnimHandle Hide(float duration, Curve easing, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            return StartHide(duration, 0, easing, autoFree);
        }

        public AnimHandle Hide(float duration, float delay, Curve easing, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            return StartHide(duration, delay, easing, autoFree);
        }

        public AnimHandle Flash(Color color, float duration, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            Graphic.color = color;
            GuiCommands.SetActive(Graphic.gameObject, true);
            return StartHide(duration, 0, Curve.Linear, autoFree);
        }

        public AnimHandle Flash(Color color, float duration, Curve easing, bool autoFree = true) {
            Game.Animation.CancelAnimation(ref SequenceHandle);
            Graphic.color = color;
            GuiCommands.SetActive(Graphic.gameObject, true);
            return StartHide(duration, 0, easing, autoFree);
        }

        #region Internals

        private AnimHandle StartShow(Color target, float duration, Curve easing) {
            Game.Animation.CancelAnimation(ref FadeHandle);

            if (duration <= 0) {
                Graphic.color = target;
                GuiCommands.SetActive(Graphic.gameObject, true);
                return default;
            }

            LiteAnimatorState state = default;
            state.ResetTime(duration);
            state.InitParamA.ColorF = target;
            state.Easing = easing;
            return (FadeHandle = Game.Animation.AddLiteAnimator(FadeAnimator, this, state));
        }

        private AnimHandle StartHide(float duration, float delay, Curve easing, bool autoFree) {
            Game.Animation.CancelAnimation(ref FadeHandle);

            if ((duration + delay) <= 0) {
                GuiCommands.SetActive(Graphic.gameObject, false);
                Graphic.color = Color.clear;
                if (autoFree && m_Allocated) {
                    GuiCommands.FreeToPool(SourcePool, this);
                }
                return default;
            }

            LiteAnimatorState state = default;
            state.ResetTimeWithDelay(duration, delay);
            state.InitParamA.ColorF = Graphic.color.WithAlpha(0);
            state.InitParamB.Bool = m_Allocated && autoFree;
            state.Easing = easing;
            return (FadeHandle = Game.Animation.AddLiteAnimator(FadeAnimator, this, state));
        }

        #endregion // Internals

        #region Anims

        // InitParamA: TargetColor

        private sealed class FadeToAnim : LiteAnimator<GuiFader> {
            public override unsafe void InitAnimation(GuiFader target, ref LiteAnimatorState state) {
                Color targetColor = state.InitParamA.ColorF;
                bool killOnFade = targetColor.a <= 0 && state.InitParamB.Bool;

                Color currentColor = target.Graphic.color;
                if (!target.Graphic.isActiveAndEnabled || target.Graphic.GetAlpha() == 0) {
                    currentColor = targetColor.WithAlpha(0);
                    target.Graphic.color = currentColor;
                }

                if (targetColor.a > 0 && !target.Graphic.gameObject.activeSelf) {
                    GuiCommands.SetActive(target.Graphic.gameObject, true);
                }

                state.InitParamA.ColorF = currentColor;
                state.InitParamB.ColorF = targetColor;
                state.StateId = killOnFade ? 1 : 0;
            }

            public override void ResetAnimation(GuiFader target, ref LiteAnimatorState state) {
            }

            public override unsafe bool UpdateAnimation(GuiFader target, ref LiteAnimatorState state, float deltaTime) {
                state.TimeRemaining -= deltaTime;
                float percent = 1 - Math.Max(0, state.TimeRemaining / state.Duration);

                target.Graphic.color = Color.LerpUnclamped(state.InitParamA.ColorF, state.InitParamB.ColorF, state.Easing.Evaluate(percent));
                if (state.TimeRemaining > 0) {
                    return true;
                }

                if (state.InitParamB.ColorF.a <= 0) {
                    GuiCommands.SetActive(target.Graphic.gameObject, false);
                    if (state.StateId == 1 && target.m_Allocated) {
                        GuiCommands.FreeToPool(target.SourcePool, target);
                    }
                }
                return false;
            }
        }

        static private readonly FadeToAnim FadeAnimator = new FadeToAnim();

        #endregion // Anims

        static public GuiFader ConstructPrefab(Transform parent) {
            GameObject go = new GameObject("GuiFader", typeof(RectTransform), typeof(CanvasRenderer), typeof(RectGraphic), typeof(GuiFader));

            RectTransform rect = (RectTransform) go.transform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = default;
            rect.anchoredPosition3D = default;
            rect.SetParent(parent, false);

            RectGraphic graphic = go.GetComponent<RectGraphic>();
            graphic.color = Color.clear;
            graphic.maskable = false;
            graphic.raycastTarget = false;

            GuiFader fader = go.GetComponent<GuiFader>();
            fader.Graphic = graphic;

            return fader;
        }

        #region IPooledObject

        void IPooledObject<GuiFader>.OnConstruct(IPool<GuiFader> inPool) {
            SourcePool = inPool;
        }

        void IPooledObject<GuiFader>.OnDestruct() {
        }

        void IPooledObject<GuiFader>.OnAlloc() {
            Graphic.color = Color.clear;
            m_Allocated = true;
        }

        void IPooledObject<GuiFader>.OnFree() {
            Game.Animation?.CancelAnimation(ref FadeHandle);
            Game.Animation?.CancelAnimation(ref SequenceHandle);
            m_Allocated = false;
        }

        #endregion // IPooledObject
    }
}