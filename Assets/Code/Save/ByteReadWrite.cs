using System;
using System.Runtime.CompilerServices;
using BeauUtil;

namespace Astro.Save {
    public struct ByteWriter {
        public unsafe byte* Head;
        public int Written;
        public int Capacity;
        public StringHash32 Tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write<T>(T val) where T : unmanaged {
            Unsafe.Write(val, ref Head, ref Written, Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteUTF8(string val) {
            Unsafe.WriteUTF8(val, ref Head, ref Written, Capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Skip(int size) {
            if (Written + size > Capacity) {
                throw new InsufficientMemoryException(string.Format("No space left for writing (size {0} vs remaining {1})", size, Capacity - Written));
            }
            Head += size;
            Written += size;
        }
    }

    public struct ByteReader {
        public unsafe byte* Head;
        public int Remaining;
        public StringHash32 Tag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T Read<T>() where T : unmanaged {
            return Unsafe.Read<T>(ref Head, ref Remaining);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Read<T>(ref T val) where T : unmanaged {
            val = Unsafe.Read<T>(ref Head, ref Remaining);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string ReadUTF8() {
            return Unsafe.ReadUTF8(ref Head, ref Remaining);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadUTF8(ref string val) {
            val = Unsafe.ReadUTF8(ref Head, ref Remaining);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Skip(int size) {
            if (Remaining < size) {
                throw new InsufficientMemoryException(string.Format("No space left for reading (size {0} vs remaining {1})", size, Remaining));
            }
            Head += size;
            Remaining -= size;
        }
    }
}