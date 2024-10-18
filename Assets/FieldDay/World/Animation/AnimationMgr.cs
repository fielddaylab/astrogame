using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Animation {
    public class AnimationMgr {
        #region Types

        private struct LiteAnimatorRecord {
            public ILiteAnimator Animator;
            public object Target;
            public LiteAnimatorState State;
            public UniqueId16 Handle;
        }

        #endregion // Types

        #region State

        private readonly UniqueIdAllocator16 m_HandleIdGenerator = new UniqueIdAllocator16(64);

        private readonly RingBuffer<LiteAnimatorRecord> m_FixedUpdateLiteAnimators = new RingBuffer<LiteAnimatorRecord>(16, RingBufferMode.Expand);
        private readonly RingBuffer<LiteAnimatorRecord> m_UpdateLiteAnimators = new RingBuffer<LiteAnimatorRecord>(16, RingBufferMode.Expand);
        private readonly RingBuffer<LiteAnimatorRecord> m_UnscaledUpdateLiteAnimators = new RingBuffer<LiteAnimatorRecord>(16, RingBufferMode.Expand);

        #endregion // State

        internal AnimationMgr() {
            
        }

        #region Lite Animators

        public AnimHandle AddLiteAnimator(ILiteAnimator animator, float duration, GameLoopPhase phase = GameLoopPhase.Update) {
            return AddLiteAnimator(animator, new LiteAnimatorState() {
                Duration = duration,
                TimeRemaining = duration
            }, phase);
        }

        public AnimHandle AddLiteAnimator(ILiteAnimator animator, object target, float duration, GameLoopPhase phase = GameLoopPhase.Update) {
            return AddLiteAnimator(animator, target, new LiteAnimatorState() {
                Duration = duration,
                TimeRemaining = duration
            }, phase);
        }

        public AnimHandle AddLiteAnimator(ILiteAnimator animator, LiteAnimatorState state, GameLoopPhase phase = GameLoopPhase.Update) {
            return AddLiteAnimator(animator, animator, state, phase);
        }

        public AnimHandle AddLiteAnimator(ILiteAnimator animator, object target, LiteAnimatorState state, GameLoopPhase phase = GameLoopPhase.Update) {
            Assert.NotNull(animator);
            var liteAnimators = GetLiteAnimators(phase);

            animator.InitAnimation(target, ref state);

            for (int i = 0; i < liteAnimators.Count; i++) {
                ref LiteAnimatorRecord record = ref liteAnimators[i];
                if (record.Animator == animator && record.Target == target) {
                    m_HandleIdGenerator.Free(record.Handle);
                    record.State = state;
                    var newId = m_HandleIdGenerator.Alloc();
                    record.Handle = newId;
                    return new AnimHandle(newId, phase, AnimationType.Lite);
                }
            }

            var id = m_HandleIdGenerator.Alloc();
            liteAnimators.PushBack(new LiteAnimatorRecord() {
                Animator = animator,
                Target = target,
                State = state,
                Handle = id
            });
            return new AnimHandle(id, phase, AnimationType.Lite);
        }

        /// <summary>
        /// Cancels an animation.
        /// </summary>
        public void CancelLiteAnimator(ILiteAnimator animator, GameLoopPhase phase = GameLoopPhase.Update) {
            CancelLiteAnimator(animator, animator, phase);
        }

        /// <summary>
        /// Cancels an animation.
        /// </summary>
        public void CancelLiteAnimator(ILiteAnimator animator, object target, GameLoopPhase phase = GameLoopPhase.Update) {
            Assert.NotNull(animator);
            var liteAnimators = GetLiteAnimators(phase);
            for (int i = 0; i < liteAnimators.Count; i++) {
                LiteAnimatorRecord animRecord = liteAnimators[i];
                if (animRecord.Animator == animator && animRecord.Target == target) {
                    animRecord.Animator.ResetAnimation(animRecord.Target, ref animRecord.State);
                    m_HandleIdGenerator.Free(animRecord.Handle);
                    liteAnimators.FastRemoveAt(i);
                    break;
                }
            }
        }

        private RingBuffer<LiteAnimatorRecord> GetLiteAnimators(GameLoopPhase phase) {
            switch (phase) {
                case GameLoopPhase.FixedUpdate:
                    return m_FixedUpdateLiteAnimators;
                case GameLoopPhase.Update:
                    return m_UpdateLiteAnimators;
                case GameLoopPhase.UnscaledUpdate:
                    return m_UnscaledUpdateLiteAnimators;
                default:
                    Assert.Fail("LiteAnimators can only be updated on FixedUpdate, Update, or UnscaledUpdate");
                    return null;
            }
        }

        #endregion // Lite Animators

        #region Cancel

        /// <summary>
        /// Cancels the animation for the given handle.
        /// </summary>
        public void CancelAnimation(AnimHandle handle) {
            CancelAnimation(ref handle);
        }

        /// <summary>
        /// Cancels the animation for the given handle.
        /// </summary>
        public void CancelAnimation(ref AnimHandle handle) {
            if (m_HandleIdGenerator.IsValid(handle.Id)) {
                int handleId = handle.Id.Id;

                switch (handle.AnimType) {
                    case AnimationType.Lite: {
                        var liteAnimators = GetLiteAnimators(handle.UpdatePhase);
                        for (int i = 0; i < liteAnimators.Count; i++) {
                            LiteAnimatorRecord animRecord = liteAnimators[i];
                            if (animRecord.Handle.Id == handleId) {
                                animRecord.Animator.ResetAnimation(animRecord.Target, ref animRecord.State);
                                m_HandleIdGenerator.Free(animRecord.Handle);
                                liteAnimators.FastRemoveAt(i);
                                break;
                            }
                        }
                        break;
                    }
                }
            }

            handle = default;
        }

        #endregion // Cancel

        #region Events

        internal void Initialize() {

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void FixedUpdateLite(float deltaTime) {
            HandleLiteUpdate(GameLoopPhase.FixedUpdate, deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLite(float deltaTime) {
            HandleLiteUpdate(GameLoopPhase.Update, deltaTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UnscaledUpdateLite(float deltaTime) {
            HandleLiteUpdate(GameLoopPhase.UnscaledUpdate, deltaTime);
        }

        private void HandleLiteUpdate(GameLoopPhase phase, float deltaTime) {
            var liteAnimators = GetLiteAnimators(phase);
            int count = liteAnimators.Count;
            while (count-- > 0) {
                LiteAnimatorRecord animRecord = liteAnimators.PopFront();
                if (animRecord.Animator.UpdateAnimation(animRecord.Target, ref animRecord.State, deltaTime)) {
                    liteAnimators.PushBack(animRecord);
                }
            }
        }

        internal void Shutdown() {
            m_UpdateLiteAnimators.Clear();
            m_UnscaledUpdateLiteAnimators.Clear();
        }

        #endregion // Events
    }
}