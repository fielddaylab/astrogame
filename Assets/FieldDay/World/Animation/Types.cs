using System.Runtime.CompilerServices;
using BeauUtil.Debugger;

namespace FieldDay.Animation {
    /// <summary>
    /// Fixed-size float array of 8 elements.
    /// </summary>
    public unsafe struct Float8 {
        public fixed float Values[8];

        public Float8(int count, float initialValue) {
            Assert.True(count <= 8);
            for(int i = 0; i < count; i++) {
                Values[i] = initialValue;
            }
        }

        public Float8(float initialValue) {
            for (int i = 0; i < 8; i++) {
                Values[i] = initialValue;
            }
        }

        public ref float this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Values[index]; }
        }

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 8; }
        }
    }

    /// <summary>
    /// Fixed-size double array of 8 elements.
    /// </summary>
    public unsafe struct Double8 {
        public fixed double Values[8];

        public Double8(int count, double initialValue) {
            Assert.True(count <= 8);
            for (int i = 0; i < count; i++) {
                Values[i] = initialValue;
            }
        }

        public Double8(double initialValue) {
            for (int i = 0; i < 8; i++) {
                Values[i] = initialValue;
            }
        }

        public ref double this[int index] {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref Values[index]; }
        }

        public int Length {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 8; }
        }
    }
}