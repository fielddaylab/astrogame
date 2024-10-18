using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio playback properties.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct AudioPropertyBlock {
        public float Volume;
        public float Pitch;
        public float Pan;
        public float LoPass;
        public float HiPass;
        public bool Pause;
        public bool Mute;

        /// <summary>
        /// Returns if these parameters result in an audible waveform.
        /// </summary>
        public readonly bool IsAudible() {
            return Volume > 0 && !Mathf.Approximately(Pitch, 0) && !Mute && !Pause;
        }

        /// <summary>
        /// Resets to the default block.
        /// </summary>
        public void Reset() {
            this = s_Default;
        }

        #region Properties

        public readonly float GetFloat(AudioFloatPropertyType property) {
            Assert.True(property >= AudioFloatPropertyType.Volume && property <= AudioFloatPropertyType.HiPass);
            unsafe {
                fixed(float* p = &Volume) {
                    return p[(int) property];
                }
            }
        }

        public void SetFloat(AudioFloatPropertyType property, float value) {
            Assert.True(property >= AudioFloatPropertyType.Volume && property <= AudioFloatPropertyType.HiPass);
            unsafe {
                fixed (float* p = &Volume) {
                    p[(int) property] = value;
                }
            }
        }

        public readonly bool GetBool(AudioBoolPropertyType property) {
            Assert.True(property >= AudioBoolPropertyType.Pause && property <= AudioBoolPropertyType.Mute);
            unsafe {
                fixed (bool* p = &Pause) {
                    return p[(int) property];
                }
            }
        }

        public void SetBool(AudioBoolPropertyType property, bool value) {
            Assert.True(property >= AudioBoolPropertyType.Pause && property <= AudioBoolPropertyType.Mute);
            unsafe {
                fixed (bool* p = &Pause) {
                    p[(int) property] = value;
                }
            }
        }

        #endregion // Properties

        #region Combinations

        /// <summary>
        /// Combines two property blocks into one.
        /// </summary>
        static public void Combine(in AudioPropertyBlock sourceA, in AudioPropertyBlock sourceB, ref AudioPropertyBlock target) {
            target.Volume = sourceA.Volume * sourceB.Volume;
            target.Pitch = sourceA.Pitch * sourceB.Pitch;
            target.Pan = sourceA.Pan + sourceB.Pan;
            target.Pause = sourceA.Pause || sourceB.Pause;
            target.Mute = sourceA.Mute || sourceB.Mute;
        }

        /// <summary>
        /// Modifies the given property block 
        /// </summary>
        static public void Mix(ref AudioPropertyBlock target, float mixFactor) {
            if (mixFactor <= 0) {
                target = s_Default;
            } else if (mixFactor < 1) {
                target.Volume = MixVal1(target.Volume, mixFactor);
                target.Pitch = MixVal1(target.Pitch, mixFactor);
                target.Pan *= mixFactor;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float MixVal1(float val, float t) {
            return 1 + (val - 1) * t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float MixVal(float defaultVal, float val, float t) {
            return defaultVal + (val - defaultVal) * t;
        }

        #endregion // Combinations

        #region Defaults

        static private readonly AudioPropertyBlock s_Default = new AudioPropertyBlock() {
            Volume = 1,
            Pitch = 1,
            Pan = 0,
            Pause = false,
            Mute = false
        };

        /// <summary>
        /// Default property block.
        /// </summary>
        static public AudioPropertyBlock Default { get { return s_Default; } }

        #endregion // Defaults
    }

    public enum AudioFloatPropertyType : byte {
        Volume,
        Pitch,
        Pan,
        LoPass,
        HiPass
    }

    public enum AudioBoolPropertyType : byte {
        Pause,
        Mute
    }
}