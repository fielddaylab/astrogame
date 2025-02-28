//#define RLE_DEBUG

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;

namespace FieldDay.Data {

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct LZCompressionHeader {
        public fixed byte Magic[4];
        public byte Version;
        public ushort Flags;
        public uint UncompressedSize;
        internal uint _Reserved;
    }

    static public unsafe class LZCompression {
        #region Checking

        /// <summary>
        /// Attempts to determine if the given buffer is compressed.
        /// </summary>
        static public bool IsCompressed(byte* ptr, uint size, out LZCompressionHeader header) {
            if (size <= sizeof(LZCompressionHeader)) {
                header = default;
                return false;
            }

            header = Unsafe.FastReinterpret<byte, LZCompressionHeader>(ptr);
            return header.Magic[0] == (byte) 'L'
                && header.Magic[1] == (byte) 'Z'
                && header.Magic[2] == (byte) 'B'
                && header.Magic[3] == (byte) '1';
        }

        /// <summary>
        /// Attempts to determine if the given buffer is compressed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool IsCompressed(UnsafeSpan<byte> span, out LZCompressionHeader header) {
            return IsCompressed(span.Ptr, (uint) span.Length, out header);
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
        private const uint MaxRunLength = (1 << 10) + MinRunLength - 1; // 1023 + 4 bytes forward
        private const uint DefaultRunLengthThreshold = 64;

        private const uint SafeStackBufferSize = 64 * Unsafe.KiB;

        private const uint MinSizeForCompression = 128;

        /// <summary>
        /// Returns if the given byte length is suitable for compression.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private bool ShouldCompress(uint size) {
            return size >= MinSizeForCompression;
        }

        /// <summary>
        /// Finds the best match to use for RLE.
        /// </summary>
        /// <param name="src">Pointer to the start of the data to match.</param>
        /// <param name="srcSize">Total length of the data that can be matched.</param>
        /// <param name="seekWindow">The maximum amount of bytes backwards to search.</param>
        /// <param name="threshold">If a run length meets or exceeds this threshold, searching will stop.</param>
        static private Match FindBestMatch(byte* src, uint srcSize, uint seekWindow, uint threshold = DefaultRunLengthThreshold) {
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

        /// <summary>
        /// Compresses the given buffer using RLE.
        /// </summary>
        static public LZCompressionResult Compress(byte* src, uint srcSize, byte* dst, uint dstSize, out uint compressedSize) {
            if (!ShouldCompress(srcSize)) {
                Unsafe.FastCopy(src, (int) srcSize, dst);
                compressedSize = srcSize;
                return LZCompressionResult.LeftUncompressed;
            }

            byte* initialSrc = src;
            byte* initialDst = dst;

            LZCompressionHeader header;
            header.Magic[0] = (byte) 'L';
            header.Magic[1] = (byte) 'Z';
            header.Magic[2] = (byte) 'B';
            header.Magic[3] = (byte) '1';
            header.Version = (byte) 1;
            header.Flags = 0;
            header.UncompressedSize = srcSize;
            header._Reserved = 0;

            int finalSize = 0;
            Unsafe.Write(header, &dst, &finalSize, (int) dstSize);

            byte groupSize = 0;
            byte groupMask = 0;
            byte* groupHeaderPtr = src;
            byte* srcEnd = src + srcSize;
            byte* dstEnd = dst + Math.Min(dstSize, srcSize);
            byte* windowStart = src;
            int windowSize = 0;
            Match match;
            int matchOffset;
            uint matchLength;

            byte* runInfo = stackalloc byte[2];

            while (src < srcEnd && dst < dstEnd) {
                if (groupSize == 0) {
                    groupSize = 8;
                    groupMask = 0;
                    groupHeaderPtr = dst++;
                }

                long length = (src - windowStart);
                windowSize = (int) Math.Min(MaxSeekWindow, length);

                if (dst + 2 <= dstEnd) {
                    match = FindBestMatch(src, (uint) (srcEnd - src), (uint) (src - initialSrc), DefaultRunLengthThreshold);
                } else {
                    match = default;
                }

                if (match.Length == 0) {
#if RLE_DEBUG
                    Log.Msg("[{0}] encoding literal: {1}", length, Unsafe.DumpMemory(src, 1, ' ', 2));
#endif // RLE_DEBUG
                    *dst++ = *src++;
                } else {
                    groupMask |= 1;
                    matchOffset = (int) (src - match.Start);
                    matchLength = match.Length;
#if RLE_DEBUG
                    Log.Msg("[{0}] encoding sequence <{1},{2}>: {3}", length, matchOffset, matchLength, Unsafe.DumpMemory(match.Start, matchLength, ' ', 2));
#endif // RLE_DEBUG

                    matchOffset -= 1;
                    matchLength -= MinRunLength;

                    runInfo[0] = (byte) ((matchOffset & 0x3FU) | ((matchLength & 0x03U) << 6));
                    runInfo[1] = (byte) ((matchLength >> 2) & 0xFF);

#if RLE_DEBUG
                    Log.Msg("wrote sequence code = {0}", Unsafe.DumpMemory(runInfo, 2, ' ', 2));
#endif // RLE_DEBUG

                    *dst++ = runInfo[0];
                    *dst++ = runInfo[1];
                    src += match.Length;
                }

                if (--groupSize == 0) {
                    // flush mask
                    *groupHeaderPtr = groupMask;
#if RLE_DEBUG
                    Log.Msg("wrote mask for previous 8 groups: {0}", groupMask);
#endif // RLE_DEBUG
                } else {
                    groupMask <<= 1;
                }
            }

            // flush mask
            if (groupSize > 0) { 
                groupMask <<= (groupSize - 1);
                *groupHeaderPtr = groupMask;
#if RLE_DEBUG
                Log.Msg("wrote mask for dangling {0} groups: {1}", 8 - groupSize, groupMask);
#endif // RLE_DEBUG
            }

            // if we didn't reach the end of the input data
            if (src < srcEnd) {
                compressedSize = 0;
                return LZCompressionResult.OutputSizeInsufficient;
            }

            finalSize = (int) (dst - initialDst);

            // if the compressed size is not smaller, then don't bother with compression
            if (finalSize >= srcSize) {
                Unsafe.Copy(initialSrc, srcSize, initialDst, dstSize);
                compressedSize = srcSize;
                return LZCompressionResult.OutputLongerThanInput;
            }

            compressedSize = (uint) finalSize;
            return LZCompressionResult.Success;
        }

        /// <summary>
        /// Compresses the given buffer using RLE.
        /// </summary>
        static public LZCompressionResult Compress(byte[] src, out byte[] dst, LZCompressionFailureStrategy failureStrategy = LZCompressionFailureStrategy.CreateNewBuffer) {
            Assert.NotNull(src);

            uint srcSize = (uint) src.Length;

            if (!ShouldCompress(srcSize)) {
                dst = failureStrategy == LZCompressionFailureStrategy.ReuseInputBuffer ? src : (byte[]) src.Clone();
                return LZCompressionResult.LeftUncompressed;
            }

            fixed (byte* srcPtr = src) {
                int dstSize = Unsafe.AlignUp32((int) srcSize + sizeof(LZCompressionHeader) + 24);
                byte* tempBuff;
                bool freeTempBuff;
                if (dstSize <= SafeStackBufferSize) {
                    byte* stackBuff = stackalloc byte[(int) dstSize];
                    tempBuff = stackBuff;
                    freeTempBuff = false;
                } else {
                    tempBuff = (byte*) Unsafe.Alloc((int) dstSize);
                    freeTempBuff = true;
                }

                try {
                    LZCompressionResult result = Compress(srcPtr, srcSize, tempBuff, (uint) dstSize, out uint compressedSize);
                    if (result == LZCompressionResult.OutputSizeInsufficient) {
                        result = LZCompressionResult.OutputLongerThanInput;
                    }

                    if (result != LZCompressionResult.Success && failureStrategy == LZCompressionFailureStrategy.ReuseInputBuffer) {
                        dst = src;
                    } else {
                        byte[] resultBuffer = new byte[compressedSize];
                        fixed (byte* resultPtr = resultBuffer) {
                            Unsafe.FastCopy(tempBuff, (int) compressedSize, resultPtr);
                        }
                        dst = resultBuffer;
                    }

                    return result;
                } finally {
                    if (freeTempBuff) {
                        Unsafe.Free(tempBuff);
                    }
                }
            }
        }

        #endregion // Compress

        #region Decompress

        static private LZDecompressionResult DecompressImpl(byte* src, uint srcSize, byte* dst, uint dstSize, LZCompressionHeader header, out uint uncompressedSize) {
            if (dstSize < header.UncompressedSize) {
                uncompressedSize = 0;
                return LZDecompressionResult.OutputSizeInsufficient;
            }

            byte* srcEnd = src + srcSize;
            byte* dstEnd = dst + header.UncompressedSize;
            byte* seekPtr;
            uncompressedSize = header.UncompressedSize;

            byte* dstStart = dst;

            byte groupCount = 0;
            byte groupMask = 0;
            byte* runInfo = stackalloc byte[2];
            int runOffset, runLength;
            while (src < srcEnd && dst < dstEnd) {
                if (groupCount == 0) {
                    groupMask = *src++;
                    groupCount = 8;
#if RLE_DEBUG
                    Log.Msg("read mask for next 8 groups: {0}", groupMask);
#endif // RLE_DEBUG
                }

#if RLE_DEBUG
                long length = (dst - dstStart);
#endif // RLE_DEBUG

                if ((groupMask & 0x80) != 0) {
                    // compressed
                    runInfo[0] = *src++;
                    runInfo[1] = *src++;

#if RLE_DEBUG
                    Log.Msg("read sequence code = {0}", Unsafe.DumpMemory(runInfo, 2, ' ', 2));
#endif // RLE_DEBUG

                    runOffset = 1 + (runInfo[0] & 0x3F);
                    runLength = (int) MinRunLength
                        + ((runInfo[0] >> 6)
                        | (runInfo[1] << 2));

                    seekPtr = dst - runOffset;

                    int lengthRemaining = runLength;
                    while (lengthRemaining-- > 0) {
                        *dst++ = *seekPtr++;
                    }

#if RLE_DEBUG
                    Log.Msg("[{0}] decoding sequence <{1},{2}>: {3}", length, runOffset, runLength, Unsafe.DumpMemory(dst - runOffset - runLength, runLength, ' ', 2));
#endif // RLE_DEBUG
                } else {
                    // literal
#if RLE_DEBUG
                    Log.Msg("[{0}] decoding literal: {1}", length, Unsafe.DumpMemory(src, 1, ' ', 2));
#endif // RLE_DEBUG
                    *dst++ = *src++;
                }

                groupMask <<= 1;
                groupCount--;
            }

            if (src == srcEnd && dst == dstEnd) {
                return LZDecompressionResult.Success;
            }

            uncompressedSize = 0;
            return LZDecompressionResult.InputNotProperlyFormatted;
        }

        /// <summary>
        /// Decompresses the given buffer using RLE.
        /// </summary>
        static public LZDecompressionResult Decompress(byte* src, uint srcSize, byte* dst, uint dstSize, out uint uncompressedSize) {
            LZCompressionHeader header;
            if (!IsCompressed(src, srcSize, out header)) {
                if (dstSize < srcSize) {
                    uncompressedSize = 0;
                    return LZDecompressionResult.OutputSizeInsufficient;
                } else {
                    Unsafe.FastCopy(src, (int) srcSize, dst);
                    uncompressedSize = srcSize;
                    return LZDecompressionResult.InputIsNotCompressed;
                }
            }

            return DecompressImpl(src + sizeof(LZCompressionHeader), (uint) (srcSize - sizeof(LZCompressionHeader)), dst, dstSize, header, out uncompressedSize);
        }

        /// <summary>
        /// Decompresses the given buffer using RLE.
        /// </summary>
        static public LZDecompressionResult Decompress(byte[] src, out byte[] dst, LZCompressionFailureStrategy failureStrategy = LZCompressionFailureStrategy.CreateNewBuffer) {
            Assert.NotNull(src);

            uint srcSize = (uint) src.Length;

            fixed (byte* srcPtr = src) {
                LZCompressionHeader header;
                if (!IsCompressed(srcPtr, srcSize, out header)) {
                    dst = failureStrategy == LZCompressionFailureStrategy.ReuseInputBuffer ? src : (byte[]) src.Clone();
                    return LZDecompressionResult.InputIsNotCompressed;
                }

                uint dstSize = Unsafe.AlignUp32(header.UncompressedSize);
                byte* tempBuff;
                bool freeTempBuff;
                if (dstSize <= SafeStackBufferSize) {
                    byte* stackBuff = stackalloc byte[(int) dstSize];
                    tempBuff = stackBuff;
                    freeTempBuff = false;
                } else {
                    tempBuff = (byte*) Unsafe.Alloc((int) dstSize);
                    freeTempBuff = true;
                }

                try {
                    LZDecompressionResult result = DecompressImpl(srcPtr + sizeof(LZCompressionHeader), (uint) (srcSize - sizeof(LZCompressionHeader)), tempBuff, dstSize, header, out dstSize);
                    if (result == LZDecompressionResult.Success) {
                        byte[] resultBuffer = new byte[dstSize];
                        fixed (byte* resultPtr = resultBuffer) {
                            Unsafe.FastCopy(tempBuff, (int) dstSize, resultPtr);
                        }
                        dst = resultBuffer;
                    } else {
                        dst = null;
                    }

                    return result;
                }
                finally {
                    if (freeTempBuff) {
                        Unsafe.Free(tempBuff);
                    }
                }
            }
        }

        #endregion // Decompress
    }

    /// <summary>
    /// Compression result.
    /// </summary>
    public enum LZCompressionResult : byte {
        Success,

        LeftUncompressed,
        OutputLongerThanInput,

        OutputSizeInsufficient, // error
    }

    /// <summary>
    /// Decompression result.
    /// </summary>
    public enum LZDecompressionResult : byte {
        Success,

        InputIsNotCompressed,

        OutputSizeInsufficient, // error
        InputNotProperlyFormatted, // error
    }

    /// <summary>
    /// Strategy to use when a buffer is unable to be compressed or decompressed.
    /// </summary>
    public enum LZCompressionFailureStrategy : byte {
        CreateNewBuffer,
        ReuseInputBuffer,
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