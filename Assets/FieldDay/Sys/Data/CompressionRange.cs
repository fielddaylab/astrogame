using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Data {
    /// <summary>
    /// Structure and helper methods for compressing floats.
    /// </summary>
    public readonly struct CompressionRange {
        public readonly float Min;
        public readonly float Max;

        public CompressionRange(float min, float max) {
            Min = min;
            Max = max;
        }

        private const byte Limit8 = 1 << 7;
        private const ushort Limit16 = 1 << 15;

        #region Byte

        /// <summary>
        /// Encodes a float to an 8-bit value.
        /// </summary>
        static public byte Encode8(CompressionRange range, float value) {
            if (value < range.Min || value > range.Max) {
                Log.Warn("[CompressionRange] Given value {0} is outside range {1}-{2}", value, range.Min, range.Max);
            }
            float inv = Mathf.Clamp01((value - range.Min) / (range.Max - range.Min));
            return (byte) (inv * Limit8);
        }

        /// <summary>
        /// Encodes a float to an 8-bit value, quantizing it first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public byte Encode8(CompressionRange range, float value, float quantize) {
            return Encode8(range, Quantize(value, quantize));
        }

        /// <summary>
        /// Decodes an 8-bit value to a float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float Decode8(CompressionRange range, byte compressed) {
            float lerp = (float) compressed / Limit8;
            return range.Min + (range.Max - range.Min) * lerp;
        }

        /// <summary>
        /// Decodes an 8-bit value to a float and quantizes it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float Decode8(CompressionRange range, byte compressed, float quantize) {
            float lerp = (float) compressed / Limit8;
            return Quantize(range.Min + (range.Max - range.Min) * lerp, quantize);
        }

        #endregion // Byte

        #region Ushort

        /// <summary>
        /// Encodes a float to a 16-bit value.
        /// </summary>
        static public ushort Encode16(CompressionRange range, float value) {
            if (value < range.Min || value > range.Max) {
                Log.Warn("[CompressionRange] Given value {0} is outside range {1}-{2}", value, range.Min, range.Max);
            }
            float inv = Mathf.Clamp01((value - range.Min) / (range.Max - range.Min));
            return (ushort) (inv * Limit16);
        }

        /// <summary>
        /// Encodes a float to a 16-bit value, quantizing it first.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public ushort Encode16(CompressionRange range, float value, float quantize) {
            return Encode16(range, Quantize(value, quantize));
        }

        /// <summary>
        /// Decodes a 16-bit value to a float.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float Decode16(CompressionRange range, ushort compressed) {
            float lerp = (float) compressed / Limit16;
            return range.Min + (range.Max - range.Min) * lerp;
        }

        /// <summary>
        /// Decodes a 16-bit value to a float and quantizes it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public float Decode16(CompressionRange range, ushort compressed, float quantize) {
            float lerp = (float) compressed / Limit16;
            return Quantize(range.Min + (range.Max - range.Min) * lerp, quantize);
        }

        #endregion // Ushort

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private float Quantize(float value, float quantize) {
            return quantize * Mathf.Round(value / quantize);
        }

        static public readonly CompressionRange ZeroToOne = new CompressionRange(0, 1);
    }
}