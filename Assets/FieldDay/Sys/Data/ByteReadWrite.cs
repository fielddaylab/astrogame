using System;
using System.Runtime.CompilerServices;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;

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
        /// Writes the given array into the buffer.
        /// </summary>
        public unsafe void WriteBuffer<T>(T[] array) where T : unmanaged {
            int size = sizeof(T) * array.Length;
            if ((Written + size) > Capacity) {
                throw new InsufficientMemoryException();
            }

            fixed (T* ptr = array) {
                Unsafe.FastCopy(ptr, size, Head);
            }

            Head += size;
            Written += size;
        }

        /// <summary>
        /// Writes the given array into the buffer.
        /// </summary>
        public unsafe void WriteBuffer<T>(T* ptr, int count) where T : unmanaged {
            int size = sizeof(T) * count;
            if ((Written + size) > Capacity) {
                throw new InsufficientMemoryException();
            }

            Unsafe.FastCopy(ptr, size, Head);

            Head += size;
            Written += size;
        }

        /// <summary>
        /// Writes the given array into the buffer.
        /// </summary>
        public unsafe void WriteBuffer(byte* ptr, int count) {
            if ((Written + count) > Capacity) {
                throw new InsufficientMemoryException();
            }

            Unsafe.FastCopy(ptr, count, Head);

            Head += count;
            Written += count;
        }

        /// <summary>
        /// Writes the given array into the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBuffer<T>(UnsafeSpan<T> array) where T : unmanaged {
            WriteBuffer(array.Ptr, array.Length);
        }

        /// <summary>
        /// Writes the given array into the buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBuffer(in ByteWriter writer) {
            // TODO: Assert that buffer does not overlap with this buffer
            Assert.False((Head - Written < (writer.Head - writer.Written + writer.Capacity))
                && (writer.Head - writer.Written) < (Head - Written + Capacity), "Cannot copy overlapping buffers");
            WriteBuffer(writer.GetData());
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Reset() {
            Head -= Written;
            Written = 0;
        }

        /// <summary>
        /// Returns the current write marker.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Copies data from the buffer into another buffer.
        /// </summary>
        public unsafe void ReadBuffer<T>(T* ptr, int count) where T : unmanaged {
            int size = count * sizeof(T);
            if (Remaining < size) {
                throw new InsufficientMemoryException();
            }

            Unsafe.FastCopy(Head, size, ptr);
            Head += size;
            Remaining -= size;
        }

        /// <summary>
        /// Copies data from the buffer into another buffer.
        /// </summary>
        public unsafe void ReadBuffer(byte* ptr, int count) {
            if (Remaining < count) {
                throw new InsufficientMemoryException();
            }

            Unsafe.FastCopy(Head, count, ptr);
            Head += count;
            Remaining -= count;
        }

        /// <summary>
        /// Copies data from the buffer into another buffer.
        /// </summary>
        public unsafe int FillBuffer(byte* ptr, int count) {
            int totalCount = Math.Min(count, Remaining);
            if (totalCount > 0) {
                Unsafe.FastCopy(Head, totalCount, ptr);
                Head += totalCount;
                Remaining -= totalCount;
            }
            return totalCount;
        }

        /// <summary>
        /// Copies data from the buffer into another buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ReadBuffer<T>(UnsafeSpan<T> array) where T : unmanaged {
            ReadBuffer(array.Ptr, array.Length);
        }

        /// <summary>
        /// Copies data from the buffer into another buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe UnsafeSpan<byte> FillBuffer(UnsafeSpan<byte> array) {
            int count = FillBuffer(array.Ptr, array.Length);
            return new UnsafeSpan<byte>(array.Ptr, count);
        }

        /// <summary>
        /// Copies data from the buffer into an array.
        /// </summary>
        public unsafe void ReadBuffer<T>(T[] array) where T : unmanaged {
            int size = array.Length * sizeof(T);
            if (Remaining < size) {
                throw new InsufficientMemoryException();
            }

            fixed (T* ptr = array) {
                Unsafe.FastCopy(Head, size, ptr);
            }
            Head += size;
            Remaining -= size;
        }

        /// <summary>
        /// Copies data from the buffer into an array.
        /// </summary>
        public unsafe int FillBuffer(byte[] array) {
            int totalCount = Math.Min(Remaining, array.Length);
            if (totalCount > 0) {
                fixed (byte* ptr = array) {
                    Unsafe.FastCopy(Head, totalCount, ptr);
                }
                Head += totalCount;
                Remaining -= totalCount;
            }
            return totalCount;
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

        /// <summary>
        /// Are we at the end of the buffer.
        /// </summary>
        public bool EOF() {
            return Remaining == 0;
        }
    }

    /// <summary>
    /// Interface for a class that can be read from a ByteReader
    /// </summary>
    public interface IByteReadable {
        void ReadFrom(ref ByteReader reader);
    }

    /// <summary>
    /// Interface for a class that can be written to a ByteWriter
    /// </summary>
    public interface IByteWritable {
        void WriteTo(ref ByteWriter writer);
    }

    /// <summary>
    /// Interface for a class that can be written to and read from a ByteWriter/Reader
    /// </summary>
    public interface IByteSerializable : IByteReadable, IByteWritable {
    }
}