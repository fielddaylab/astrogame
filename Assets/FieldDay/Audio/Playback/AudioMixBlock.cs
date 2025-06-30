#if !UNITY_WEBGL
#define SUPPORTS_AUDIOEFFECTS
#endif // !UNITY_WEBGL

using System.Runtime.CompilerServices;
using TinyIL;

namespace FieldDay.Audio {
    internal struct AudioMixBlock {
        public unsafe fixed float Volume[AudioMgr.MaxBuses];
        public unsafe fixed float Pitch[AudioMgr.MaxBuses];
        public unsafe fixed float Pan[AudioMgr.MaxBuses];

        public unsafe fixed float LoPass[AudioMgr.MaxBuses];
        public unsafe fixed float HiPass[AudioMgr.MaxBuses];

        public unsafe void Reset() {
            for(int i = 0; i < AudioMgr.MaxBuses; i++) {
                Volume[i] = Pitch[i] = 1;
                Pan[i] = 0;

                LoPass[i] = HiPass[i] = 0;
            }
        }

        /// <summary>
        /// Mixes a multiplier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldc.r4 1; ldarg.0; ldc.r4 1; sub; ldarg.1; mul; add; ret")]
        static public float MixMultiplier(float multiplier, float factor) {
            return 1 + (multiplier - 1) * factor;
        }

        /// <summary>
        /// Multiplies a value with a mixed multiplier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [IntrinsicIL("ldarg.0; ldc.r4 1; ldarg.1; ldc.r4 1; sub; ldarg.2; mul; add; mul; ret")]
        static public float MixedMultiply(float value, float multiplier, float factor) {
            return value * (1 + (multiplier - 1) * factor);
        }
    }
}