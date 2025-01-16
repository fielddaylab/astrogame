using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Animation {
    public interface ILiteAnimator {
        void InitAnimation(object target, ref LiteAnimatorState state);
        bool UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(object target, ref LiteAnimatorState state);
    }

    public interface ILiteAnimator<T> : ILiteAnimator where T : class {
        void InitAnimation(T target, ref LiteAnimatorState state);
        bool UpdateAnimation(T target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(T target, ref LiteAnimatorState state);
    }

    public abstract class LiteAnimator<T> : ILiteAnimator<T> where T : class {
        public abstract void InitAnimation(T target, ref LiteAnimatorState state);

        public abstract void ResetAnimation(T target, ref LiteAnimatorState state);

        public abstract bool UpdateAnimation(T target, ref LiteAnimatorState state, float deltaTime);

        void ILiteAnimator.InitAnimation(object target, ref LiteAnimatorState state) {
            InitAnimation(Unsafe.FastCast<T>(target), ref state);
        }

        void ILiteAnimator.ResetAnimation(object target, ref LiteAnimatorState state) {
            ResetAnimation(Unsafe.FastCast<T>(target), ref state);
        }

        bool ILiteAnimator.UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime) {
            return UpdateAnimation(Unsafe.FastCast<T>(target), ref state, deltaTime);
        }
    }

    public struct LiteAnimatorState {
        public float TimeRemaining;
        public float Duration;
        public Curve Easing;
        public ushort Flags;
        public int StateId;
        public LiteAnimatorStateParam InitParamA;
        public LiteAnimatorStateParam InitParamB;
        public LiteAnimatorStateParam StateParam;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetTime(float duration) {
            TimeRemaining = Duration = duration;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetTimeWithDelay(float duration, float delay) {
            Duration = duration;
            TimeRemaining = duration + delay;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ScaleTime(float scale) {
            TimeRemaining *= scale;
            Duration *= scale;
        }

        public float PercentRemaining {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Math.Max(0, TimeRemaining / Duration); }
        }

        public float PercentProgress {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 1f - Math.Max(0, TimeRemaining / Duration); }
        }

        public bool IsStarted {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return TimeRemaining < Duration; }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct LiteAnimatorStateParam {
        [FieldOffset(0)] public bool Bool;
        [FieldOffset(0)] public int Int;
        [FieldOffset(0)] public float Float;
        [FieldOffset(0)] public Vector2 Float2;
        [FieldOffset(0)] public Vector3 Float3;
        [FieldOffset(0)] public Vector4 Float4;
        [FieldOffset(0)] public Quaternion Quaternion;
        [FieldOffset(0)] public RuntimeObjectHandle Object;
    }
}