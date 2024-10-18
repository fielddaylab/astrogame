using System;
using System.Runtime.CompilerServices;
using BeauUtil;
using Unity.IL2CPP.CompilerServices;
using UnityEngine;

namespace FieldDay.Animation {
    static public class AnimUtility {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool CrossedThreshold(float currentTime, float desiredTime, float deltaTime) {
            return Math.Sign(currentTime - desiredTime) != Math.Sign(currentTime - deltaTime - desiredTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool CrossedThresholdNormalized(float currentTime, float duration, float normalizedValue, float deltaTime) {
            return CrossedThreshold(currentTime, duration * normalizedValue, deltaTime);
        }

        #region Frame Ranges

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame ranges.
        /// </summary>
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public bool FrameInRange(ushort frameIndex, OffsetLengthU16[] ranges) {
            for(int i = 0, len = ranges.Length; i < len; i++) {
                if (ranges[i].Contains(frameIndex)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame range,
        /// and outputs the interpolation within that range.
        /// </summary>
        static public bool FrameInRange(ushort frameIndex, OffsetLengthU16 range, out float lerp) {
            float rawLerp = (frameIndex - range.Offset) / (float) (range.Length);
            bool inRange = rawLerp >= 0 && rawLerp < 1;
            lerp = Mathf.Clamp01(rawLerp);
            return inRange;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame ranges.
        /// </summary>
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public bool FrameInRange(short frameIndex, OffsetLength16[] ranges) {
            for (int i = 0, len = ranges.Length; i < len; i++) {
                if (ranges[i].Contains(frameIndex)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame range,
        /// and outputs the interpolation within that range.
        /// </summary>
        static public bool FrameInRange(short frameIndex, OffsetLength16 range, out float lerp) {
            float rawLerp = (frameIndex - range.Offset) / (float) (range.Length);
            bool inRange = rawLerp >= 0 && rawLerp < 1;
            lerp = Mathf.Clamp01(rawLerp);
            return inRange;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame ranges.
        /// </summary>
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public bool FrameInRange(uint frameIndex, OffsetLengthU32[] ranges) {
            for (int i = 0, len = ranges.Length; i < len; i++) {
                if (ranges[i].Contains(frameIndex)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame range,
        /// and outputs the interpolation within that range.
        /// </summary>
        static public bool FrameInRange(uint frameIndex, OffsetLengthU32 range, out float lerp) {
            float rawLerp = (frameIndex - range.Offset) / (float) (range.Length);
            bool inRange = rawLerp >= 0 && rawLerp < 1;
            lerp = Mathf.Clamp01(rawLerp);
            return inRange;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame ranges.
        /// </summary>
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        static public bool FrameInRange(int frameIndex, OffsetLength32[] ranges) {
            for (int i = 0, len = ranges.Length; i < len; i++) {
                if (ranges[i].Contains(frameIndex)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given frame is within the bounds of the provided frame range,
        /// and outputs the interpolation within that range.
        /// </summary>
        static public bool FrameInRange(int frameIndex, OffsetLength32 range, out float lerp) {
            float rawLerp = (frameIndex - range.Offset) / (float) (range.Length);
            bool inRange = rawLerp >= 0 && rawLerp < 1;
            lerp = Mathf.Clamp01(rawLerp);
            return inRange;
        }

        #endregion // Frame Ranges

        /// <summary>
        /// Returns the number of frames in a given clip.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public int FrameCount(this AnimationClip clip) {
            return Mathf.CeilToInt(clip.length * clip.frameRate);
        }

        static public class Editor {
            /// <summary>
            /// Returns a reference to the clip for the given state behavior.
            /// </summary>
            static public AnimationClip FindClipForState(StateMachineBehaviour behavior) {
#if UNITY_EDITOR
                var contexts = UnityEditor.Animations.AnimatorController.FindStateMachineBehaviourContext(behavior);
                if (contexts != null && contexts.Length > 0) {
                    UnityEditor.Animations.AnimatorState state = contexts[0].animatorObject as UnityEditor.Animations.AnimatorState;
                    if (state != null) {
                        AnimationClip clip = state.motion as AnimationClip;
                        return clip;
                    }
                }
                return null;
#else
                throw new NotSupportedException();
#endif // UNITY_EDITOR
            }

            /// <summary>
            /// Caches a reference to the clip for the given state behavior.
            /// </summary>
            static public AnimationClip CacheClip(StateMachineBehaviour behavior, ref AnimationClip clip) {
#if UNITY_EDITOR
                if (clip == null) {
                    clip = FindClipForState(behavior);
                }
                return clip;
#else
                throw new NotSupportedException();
#endif // UNITY_EDITOR
            }

            /// <summary>
            /// Caches the number of frames in the clip for the given state behavior.
            /// </summary>
            static public int CacheClipFrames(StateMachineBehaviour behavior, ref int frames) {
#if UNITY_EDITOR
                if (frames == 0) {
                    AnimationClip clip = FindClipForState(behavior);
                    if (clip) {
                        frames = clip.FrameCount();
                    } else {
                        frames = 1;
                    }
                }
                return frames;
#else
                throw new NotSupportedException();
#endif // UNITY_EDITOR
            }
        }

    }
}