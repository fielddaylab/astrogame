using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Animation {
    public interface ILiteAnimator {
        void InitAnimation(object target, ref LiteAnimatorState state);
        bool UpdateAnimation(object target, ref LiteAnimatorState state, float deltaTime);
        void ResetAnimation(object target, ref LiteAnimatorState state);
    }

    public struct LiteAnimatorState {
        public float TimeRemaining;
        public float Duration;
        public BitSet32 Flags;
        public int StateId;
        public LiteAnimatorStateParam InitParam;
        public LiteAnimatorStateParam StateParam;
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