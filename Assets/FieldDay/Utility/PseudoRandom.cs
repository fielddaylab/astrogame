using BeauUtil;
using UnityEngine.UIElements;

namespace FieldDay {
    public struct PseudoRandom {
        public uint Seed;

        public PseudoRandom(uint seed) {
            Seed = seed;
        }

        public PseudoRandom(StringHash32 seed) {
            Seed = seed.HashValue;
        }

        public int Int(int range, uint mod = 0) {
            return Int(ref Seed, range, mod);
        }

        public float Float(float min, float max, uint mod = 0) {
            return Float(ref Seed, min, max, mod);
        }

        public bool Bool(float chance, uint mod = 0) {
            return Int(ref Seed, ushort.MaxValue) >= (int) (chance * ushort.MaxValue);
        }

        public bool Bool(uint mod = 0) {
            return Int(ref Seed, ushort.MaxValue) >= ushort.MaxValue / 2;
        }

        public unsafe void Shuffle<T>(UnsafeSpan<T> span, uint mod = 0) where T : unmanaged {
            Shuffle(span.Ptr, span.Length, mod);
        }

        public unsafe void Shuffle<T>(T* ptr, int length, uint mod = 0) where T : unmanaged {
            int i = length, j;
            while (--i > 0) {
                T old = ptr[i];
                ptr[i] = ptr[j = Int(i + 1, mod)];
                ptr[j] = old;
            }
        }

        static public int Int(ref uint seed, int range, uint mod = 0) {
            seed = (uint) (((ulong) seed * 48271 * (mod + 1)) % 0x7FFFFFFF);
            return (int) (seed % range);
        }

        static public float Float(ref uint seed, float min, float max, uint mod = 0) {
            float rand = Int(ref seed, ushort.MaxValue, mod) / (float) ushort.MaxValue;
            return min + (max - min) * rand;
        }
    }
}