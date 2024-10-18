#define RLE_DEBUG

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauData;
using BeauUtil;

namespace FieldDay.Data {

    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public unsafe struct LZCompressionHeader {
        public fixed byte Magic[4];
        public byte Version;
        public byte Flags;
        public uint UncompressedSize;
    }

    static public unsafe class LZCompression {
        #region Checking

        /// <summary>
        /// Attempts to determine if the given buffer is compressed.
        /// </summary>
        static public bool IsCompressed(byte* ptr, int size, out LZCompressionHeader header) {
            if (size <= sizeof(LZCompressionHeader)) {
                header = default;
                return false;
            }

            header = Unsafe.FastReinterpret<byte, LZCompressionHeader>(ptr);
            return header.Magic[0] == 'L' && header.Magic[1] == 'Z' && header.Magic[2] == 'B' && header.Magic[3] == '1';
        }

        /// <summary>
        /// Attempts to determine if the given buffer is compressed.
        /// </summary>
        static public bool IsCompressed(UnsafeSpan<byte> span, out LZCompressionHeader header) {
            return IsCompressed(span.Ptr, span.Length, out header);
        }

        #endregion // Checking

        #region Compress

        /// <summary>
        /// Run-length match.
        /// </summary>
        public unsafe struct Match {
            public byte* Start;
            public uint Length;
        }

        private const uint MaxSeekWindow = 1 << 6; // 64 bytes back
        private const uint MinRunLength = 4; // minimum bytes to copy to be counted
        private const uint MaxRunLength = (1 << 10) + MinRunLength; // 1024 + 4 bytes forward
        private const uint DefaultRunLengthThreshold = 64;

        private const uint SeekMask = (1 << 6) - 1;
        private const uint LengthMask = (1 << 10) - 1;

        private const uint MinSizeForCompression = 128;

        /// <summary>
        /// Returns if the given byte length is suitable for compression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool ShouldCompress(uint size) {
            return size >= MinSizeForCompression;
        }

        /// <summary>
        /// Finds the best match to use for RLE.
        /// </summary>
        /// <param name="src">Pointer to the start of the data to match.</param>
        /// <param name="srcSize">Total length of the data that can be matched.</param>
        /// <param name="seekWindow">The maximum amount of bytes backwards to search.</param>
        /// <param name="threshold">If a run length meets or exceeds this threshold, searching will stop.</param>
        static public Match FindBestMatch(byte* src, uint srcSize, uint seekWindow, uint threshold = DefaultRunLengthThreshold) {
            if (seekWindow < MinRunLength) {
                return default;
            }

            seekWindow = Math.Min(seekWindow, MaxSeekWindow);
            srcSize = Math.Min(srcSize, MaxRunLength);
            threshold = Math.Min(threshold, srcSize);

            byte* bestStart = null;
            uint bestLength = 0;

            byte* seekPtrStart, seekPtr, seekEnd, compPtr, compEnd;
            compEnd = src + srcSize;
            
            for(int i = 1; i <= seekWindow; i++) {
                seekPtrStart = src - i;
                seekPtr = seekPtrStart;
                seekEnd = seekPtr + srcSize;
                compPtr = src;

                while(seekPtr < seekEnd && compPtr < compEnd) {
                    if (*seekPtr != *compPtr) {
                        break;
                    }

                    seekPtr++;
                    compPtr++;
                }

                uint runLength = (uint) (seekPtr - seekPtrStart);
                if (runLength >= MinRunLength && runLength > bestLength) {
                    bestLength = runLength;
                    bestStart = seekPtrStart;

                    if (runLength >= threshold) {
                        break;
                    }
                }
            }

            return new Match() {
                Start = bestStart,
                Length = bestLength
            };
        }

        static public LZCompressionResult Compress(byte* src, uint srcSize, byte* dst, uint dstSize, out uint compressedSize) {
            //if (!ShouldCompress(srcSize)) {
            //    Unsafe.FastCopy(src, (int) srcSize, dst);
            //    compressedSize = srcSize;
            //    return LZCompressionResult.LeftUncompressed;
            //}
            compressedSize = 0;
            return LZCompressionResult.LeftUncompressed;
        }

        /// <summary>
        /// Enumerator that compresses one block of data per iteration.
        /// </summary>
        public unsafe struct CompressEnumerator : IEnumerator, IEnumerable, IDisposable {
            private const int Phase_Begin = 0;
            private const int Phase_Blocks = 1;
            private const int Phase_Complete = 2;
            private const int Phase_Done = 3;
            
            private byte* m_SrcStart;
            private byte* m_SrcCurrent;
            private uint m_SrcSize;
            private byte* m_DstStart;
            private byte* m_DstCurrent;
            private uint m_DstSize;
            private LZCompressCallback m_Callback;
            private int m_Phase;

            #region IEnumerable

            public CompressEnumerator GetEnumerator() {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return this;
            }

            #endregion // IEnumerable

            #region IDisposable

            public void Dispose() {
                m_SrcStart = null;
                m_SrcCurrent = null;
                m_SrcSize = 0;
                m_DstStart = null;
                m_DstCurrent = null;
                m_DstSize = 0;
                m_Callback = null;
                m_Phase = Phase_Done;
            }

            #endregion // IDisposable

            #region IEnumerator

            public object Current { get { return null; } }

            public bool MoveNext() {
                switch (m_Phase) {
                    case Phase_Begin: {
                        if (!ShouldCompress(m_SrcSize)) {
                            if (m_DstSize < m_SrcSize) {
                                EndWithResult(LZCompressionResult.OutputSizeInsufficient, 0);
                            } else {
                                Unsafe.FastCopy(m_SrcStart, (int) m_SrcSize, m_DstStart);
                                EndWithResult(LZCompressionResult.LeftUncompressed, m_SrcSize);
                            }
                        } else {
                            if (m_DstSize < sizeof(LZCompressionHeader)) {
                                EndWithResult(LZCompressionResult.OutputSizeInsufficient, 0);
                            }

                            LZCompressionHeader header;
                            header.Magic[0] = (byte) 'L';
                            header.Magic[1] = (byte) 'Z';
                            header.Magic[2] = (byte) 'B';
                            header.Magic[3] = (byte) '1';
                        }
                        break;
                    }
                }

                return m_Phase != Phase_Done;
            }

            private void EndWithResult(LZCompressionResult result, uint size) {
                m_Callback(result, size);
                m_Phase = Phase_Done;
            }

            void IEnumerator.Reset() {
                throw new NotSupportedException();
            }

            #endregion // IEnumerator
        }

        #endregion // Compress

        #region Decompress

        #endregion // Decompress
    }

    /// <summary>
    /// Compression or decompression result.
    /// </summary>
    public enum LZCompressionResult : byte {
        Success,
        LeftUncompressed,
        OutputLongerThanInput,
        OutputSizeInsufficient,
        InputNotProperlyFormatted,
        InputIsNotCompressed
    }

    /// <summary>
    /// Callback when compression is completed.
    /// </summary>
    public delegate void LZCompressCallback(LZCompressionResult result, uint compressedSize);
    
    /// <summary>
    /// Callback when decompression is completed.
    /// </summary>
    public delegate void LZDecompressCallback(LZCompressionResult result, LZCompressionHeader header, uint uncompressedSize);
}