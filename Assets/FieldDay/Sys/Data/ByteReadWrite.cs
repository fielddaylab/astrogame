using System;
using System.Runtime.CompilerServices;
using BeauUtil;

namespace FieldDay.Data {
    /// <summary>
    /// Simple byte writer.
    /// </summary>
    public struct ByteWriter {
        public unsafe byte* Head;
        public int Written;
        public int Capacity;

        public unsafe ByteWriter(byte* head, int capacity) {
            Head = head;
            Written = 0;
            Capacity = capacity;
        }

        /// <summary>
        /// Writes the given data to the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Write<T>(in T val) where T : unmanaged {
            Unsafe.Write(val, ref Head, ref Written, Capacity);
        }

        /// <summary>
        /// Overwrites data at the given marker.
        /// </summary>
        public unsafe void Overwrite<T>(T val, uint marker) where T : unmanaged {
            if (marker + sizeof(T) > Capacity) {
                throw new InsufficientMemoryException();
            }

            Unsafe.FastCopy(&val, sizeof(T), Head - Written + marker);
        }

        /// <summary>
        /// Writes the given string to the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteUTF8(string val) {
            Unsafe.WriteUTF8(val, ref Head, ref Written, Capacity);
        }

        /// <summary>
        /// Skips the given number of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Skip(int size) {
            if (Written + size > Capacity) {
                throw new InsufficientMemoryException();
            }
            Head += size;
            Written += size;
        }

        /// <summary>
        /// Pads the buffer with the given number of zeroes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Pad(int size) {
            if (Written + size > Capacity) {
                throw new InsufficientMemoryException();
            }
            int remaining = size;
            while(remaining-- > 0) {
                *Head++ = 0;
            }
            Written += size;
        }

        /// <summary>
        /// Resets the buffer to its head.
        /// </summary>
        public unsafe void Reset() {
            Head -= Written;
            Written = 0;
        }

        /// <summary>
        /// Returns the current write marker.
        /// </summary>
        public uint GetMarker() {
            return (uint) Written;
        }

        /// <summary>
        /// Returns the written data as a byte span.
        /// </summary>
        public unsafe UnsafeSpan<byte> GetData() {
            return new UnsafeSpan<byte>(Head - Written, Written);
        }

        /// <summary>
        /// Returns a copy of the written data.
        /// </summary>
        public unsafe byte[] GetDataCopy() {
            byte[] bytes = new byte[Written];
            Unsafe.CopyArray(Head - Written, Written, bytes);
            return bytes;
        }
    }

    /// <summary>
    /// Simple byte reader.
    /// </summary>
    public struct ByteReader {
        public unsafe byte* Head;
        public int Remaining;

        public unsafe ByteReader(byte* head, int size) {
            Head = head;
            Remaining = size;
        }

        public unsafe ByteReader(UnsafeSpan<byte> span) {
            Head = span.Ptr;
            Remaining = span.Length;
        }

        /// <summary>
        /// Reads data from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe T Read<T>() where T : unmanaged {
            return Unsafe.Read<T>(ref Head, ref Remaining);
        }

        /// <summary>
        /// Reads data from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Read<T>(ref T val) where T : unmanaged {
            val = Unsafe.Read<T>(ref Head, ref Remaining);
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe string ReadUTF8() {
            return Unsafe.ReadUTF8(ref Head, ref Remaining);
        }

        /// <summary>
        /// Reads a string from the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadUTF8(ref string val) {
            val = Unsafe.ReadUTF8(ref Head, ref Remaining);
        }

        /// <summary>
        /// Skips the given number of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Skip(int size) {
            if (Remaining < size) {
                throw new InsufficientMemoryException();
            }

            Head += size;
            Remaining -= size;
        }

        /// <summary>
        /// Skips the given number of bytes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Pad(int size) {
            Skip(size);
        }
    }
}