using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Data;
using FieldDay.Debugging;
using UnityEngine;

namespace Astro.Save {
    public class SaveMgr {
        public const int SaveVersion = 3;

        private struct ChunkRecord {
            public StringHash32 Id;
            public uint UncompressedSize;
            public UnsafeSpan<byte> Data;
        }

        private struct ChunkReader {
            public SaveStateChunkReader Reader;
            public object Context;
        }

        private struct ChunkWriter {
            public StringHash32 Id;
            public int Order;
            public SaveStateChunkWriter Writer;
            public object Context;
        }

        private struct SaveRecord {
            public long Timestamp;
            public UnsafeSpan<byte> Data;
        }

        public const int MainBufferSize = 80 * Unsafe.KiB; // 80k buffer
        public const int ChunkBufferSize = 64 * Unsafe.KiB; // 64k buffer
        public const int ScratchBufferSize = 32 * Unsafe.KiB; // 32k buffer
        public const int CharBufferSize = 256 * Unsafe.KiB; // 256k for chars
        public const int TotalBufferSize = (MainBufferSize * 2) + ChunkBufferSize + ScratchBufferSize + CharBufferSize;

        private Unsafe.ArenaHandle m_BufferArena;
        private unsafe byte* m_MainBuffer;
        private unsafe byte* m_UncommittedBuffer;
        private unsafe byte* m_ChunkBuffer;
        private unsafe char* m_CharBuffer;
        private unsafe Unsafe.ArenaHandle m_ScratchBuffer;

        private int m_UsedSize;
        private int m_CharsWritten;
        private int m_UncommittedSize;

        private SaveStateHeader m_CurrentHeader;
        private SaveStateChunkConsts m_CurrentConsts;
        private SaveScratchpad m_CurrentScratch;
        private RingBuffer<ChunkRecord> m_ChunkRecords = new RingBuffer<ChunkRecord>(32, RingBufferMode.Expand);
        private StringHash32 m_ActiveChunk;

        private readonly RingBuffer<ChunkWriter> m_ChunkWriters = new RingBuffer<ChunkWriter>(32, RingBufferMode.Expand);
        private bool m_WriterOrderDirty = false;

        private readonly Dictionary<StringHash32, ChunkReader> m_ChunkReaders;
        private readonly RingBuffer<ISaveStatePostLoad> m_PostLoadHandlers = new RingBuffer<ISaveStatePostLoad>(32, RingBufferMode.Expand);

        public SaveMgr() {
            m_ChunkReaders = MapUtils.Create<StringHash32, ChunkReader>(32);
            Allocate();
        }

        public unsafe void Allocate() {
            Free();
            m_BufferArena = Unsafe.CreateArena(TotalBufferSize + 128, "Save");
            m_MainBuffer = (byte*) m_BufferArena.AllocAligned(MainBufferSize, 8);
            m_UncommittedBuffer = (byte*) m_BufferArena.AllocAligned(MainBufferSize, 8);
            m_ChunkBuffer = (byte*) m_BufferArena.AllocAligned(ChunkBufferSize, 8);
            m_CharBuffer = m_BufferArena.AllocArray<char>(CharBufferSize / 2);
            m_ScratchBuffer = Unsafe.CreateArena(m_BufferArena, ScratchBufferSize);
        }

        public unsafe void Free() {
            if (Unsafe.TryDestroyArena(ref m_BufferArena)) {
                m_MainBuffer = null;
                m_UncommittedBuffer = null;
                m_ChunkBuffer = null;
                m_CharBuffer = null;
                m_ScratchBuffer = default;
            }
        }

        #region Handlers

        public unsafe void RegisterHandler(StringHash32 id, ISaveStateChunkObject chunkObj, int order = 0) {
            RegisterHandler(id, chunkObj, chunkObj.Write, chunkObj.Read, order);
        }

        public unsafe void RegisterHandler(StringHash32 id, object context, SaveStateChunkWriter writer, SaveStateChunkReader reader, int order = 0) {
            m_ChunkWriters.PushBack(new ChunkWriter() {
                Context = context,
                Id = id,
                Writer = writer,
                Order = order
            });
            m_WriterOrderDirty = true;

            m_ChunkReaders.Add(id, new ChunkReader() {
                Context = context,
                Reader = reader
            });
        }

        public void RegisterPostLoad(ISaveStatePostLoad postLoad) {
            m_PostLoadHandlers.PushBack(postLoad);
        }

        public void DeregisterHandler(StringHash32 id) {
            m_ChunkReaders.Remove(id);

            int chunkWriterIdx = m_ChunkWriters.FindIndex((w, i) => w.Id == i, id);
            if (chunkWriterIdx >= 0) {
                m_ChunkWriters.FastRemoveAt(chunkWriterIdx);
                m_WriterOrderDirty = true;
            }
        }

        public void DeregisterPostLoad(ISaveStatePostLoad postLoad) {
            m_PostLoadHandlers.FastRemove(postLoad);
        }

        #endregion // Handlers

        #region Write

        public unsafe UnsafeSpan<byte> GetRawBytes() {
            return new UnsafeSpan<byte>(m_MainBuffer, (uint) m_UsedSize);
        }

        public void Write(SaveSlot slot) {
            SaveStateHeader header;
            header.PlayerCode = Game.SharedState.Get<UserSettingsState>().PlayerCode;
            header.LastSaveTS = DateTime.UtcNow.ToFileTimeUtc();
            header.Playtime = 0;
            header.Version = SaveVersion;

            SaveStateChunkConsts consts;
            consts.Version = SaveVersion;

            Write(header, consts, slot);
        }

        public unsafe void Write(SaveStateHeader header, SaveStateChunkConsts consts, SaveSlot slot) {
            byte* head = slot == SaveSlot.Uncommitted ? m_UncommittedBuffer : m_MainBuffer;

            ByteWriter writer;
            writer.Head = head;
            writer.Capacity = MainBufferSize;
            writer.Written = 0;

            m_ChunkRecords.Clear();

            if (m_WriterOrderDirty) {
                m_ChunkWriters.Sort((a, b) => a.Order - b.Order);
                m_WriterOrderDirty = false;
            }

            writer.WriteUTF8(header.PlayerCode);
            writer.Write(header.LastSaveTS);
            writer.Write(header.Playtime);
            writer.Write(header.Version);

            writer.Skip(1); // padding

            m_CurrentHeader = header;

            byte* manifestWriteMarker = writer.Head;

            SaveStateManifest manifest;
            manifest.ChunkCount = (uint) m_ChunkWriters.Count;
            manifest.Checksum = 0;
            manifest.Length = 0;
            int padding = 8 - (int) (((ulong) writer.Head + (ulong) sizeof(SaveStateManifest)) % 8);
            manifest.Padding = (byte) padding;

            writer.Write(manifest);
            
            while(padding-- > 0) {
                writer.Write((byte) 0);
            }

            m_ScratchBuffer.Reset();

            SaveScratchpad scratch;
            scratch.Allocator = m_ScratchBuffer;
            scratch.BlockCount = 0;
            scratch.Blocks = m_ScratchBuffer.AllocSpan<SaveScratchBlock>(32);
            m_CurrentScratch = scratch;

            byte* manifestLengthChecksumMarker = writer.Head;
            int manifestLengthCalcMarker = writer.Written;

            foreach(var chunk in m_ChunkWriters) {
                byte* chunkHeaderWriteMarker = writer.Head;

                SaveStateChunkHeader chunkHeader;
                chunkHeader.Id = chunk.Id;
                chunkHeader.ChunkLength = 0;
                chunkHeader.ChunkLengthUncompressed = 0;

                writer.Write(chunkHeader);

                ByteWriter chunkWriter;
                chunkWriter.Head = m_ChunkBuffer;
                chunkWriter.Written = 0;
                chunkWriter.Capacity = ChunkBufferSize;

                chunk.Writer(chunk.Context, ref chunkWriter, consts, ref m_CurrentScratch);

                byte* chunkDataStart = writer.Head;
                int compressedSize;
                int uncompressedSize = chunkWriter.Written;

                // TODO: compress here if desired
                bool compressed = Compress(m_ChunkBuffer, chunkWriter.Written, chunkDataStart, writer.Capacity - writer.Written, &compressedSize);

                writer.Head += compressedSize;
                writer.Written += compressedSize;

                chunkHeader.ChunkLength = (uint) uncompressedSize;
                chunkHeader.ChunkLengthUncompressed = (uint) chunkWriter.Written;

                Unsafe.Copy(&chunkHeader, sizeof(SaveStateChunkHeader), chunkHeaderWriteMarker);

                m_ChunkRecords.PushBack(new ChunkRecord() {
                    Id = chunkHeader.Id,
                    Data = new UnsafeSpan<byte>(chunkDataStart, chunkHeader.ChunkLength),
                    UncompressedSize = chunkHeader.ChunkLengthUncompressed
                });

                Log.Msg("[SaveMgr] Wrote chunk '{0}' ({1}, {2} uncompressed)", chunk.Id, Unsafe.FormatBytes(chunkHeader.ChunkLength), Unsafe.FormatBytes(chunkHeader.ChunkLengthUncompressed));
                //Log.Msg("...compressed " + Unsafe.DumpMemory(chunkDataStart, chunkHeader.ChunkLength, ' ', 2));
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(m_ChunkBuffer, chunkWriter.Written, ' ', 2));
            }

            manifest.Length = (uint) (writer.Written - manifestLengthCalcMarker);
            manifest.Checksum = Unsafe.Hash64(manifestLengthChecksumMarker, (int) manifest.Length);

            Log.Msg("[SaveMgr] Calculated checksum from <{0},{1}> = {2}", manifestLengthChecksumMarker - head, manifest.Length, manifest.Checksum); ;

            Unsafe.Copy(&manifest, sizeof(SaveStateManifest), manifestWriteMarker);

            if (slot == SaveSlot.Uncommitted) {
                m_UncommittedSize = writer.Written;
            } else {
                m_UsedSize = writer.Written;
                m_UncommittedSize = 0; // clear uncommitted if we do a main save
            }

            Log.Msg("[SaveMgr] Wrote save data ({0} chunks, {1}) to slot {2}", manifest.ChunkCount, Unsafe.FormatBytes(m_UsedSize), slot);

            if (!string.IsNullOrEmpty(header.PlayerCode)) {
                PlayerPrefs.SetString("LatestPlayerCode", header.PlayerCode);
                PlayerPrefs.Save();
            }
        }

        static unsafe public bool Compress(byte* src, int srcSize, byte* dest, int destSize, int* compressedSize)
        {
            Unsafe.Copy(src, srcSize, dest, destSize);
            *compressedSize = srcSize;
            return false;
        }

        public void Clear() {
            m_CurrentHeader = default;
            m_UsedSize = 0;
            m_UncommittedSize = 0;
            m_CurrentConsts = default;
        }

        public bool HasUncommittedSave {
            get { return m_UncommittedSize > 0; }
        }

        public unsafe bool TryCommitSave() {
            if (m_UncommittedSize > 0) {
                Unsafe.Copy(m_UncommittedBuffer, m_UncommittedSize, m_MainBuffer, MainBufferSize);
                m_UsedSize = m_UncommittedSize;
                m_UncommittedSize = 0;
                Log.Msg("[SaveMgr] Moved uncommitted save data to main save data");
                return true;
            }
            return false;
        }

        #endregion // Write

        #region Read

        public bool HasSave {
            get { return m_UsedSize > 0; }
        }

        public unsafe UnsafeSpan<byte> GetRawCopyDest() {
            return new UnsafeSpan<byte>(m_MainBuffer, MainBufferSize);
        }

        public string SaveCode {
            get { return m_CurrentHeader.PlayerCode; }
        }

        public unsafe bool Read() {
            return Read(new UnsafeSpan<byte>(m_MainBuffer, (uint) m_UsedSize));
        }

        public unsafe bool Read(UnsafeSpan<byte> bytes) {
            if (bytes.Ptr != m_MainBuffer) {
                Unsafe.Copy(bytes.Ptr, bytes.Length, m_MainBuffer, MainBufferSize);
            }

            m_UsedSize = bytes.Length;

            ByteReader reader;
            reader.Head = m_MainBuffer;
            reader.Remaining = bytes.Length;

            SaveStateHeader header;
            header.PlayerCode = reader.ReadUTF8();
            header.LastSaveTS = reader.Read<long>();
            header.Playtime = reader.Read<double>();
            header.Version = reader.Read<int>();
            m_CurrentHeader = header;

            Game.SharedState.Get<UserSettingsState>().PlayerCode = header.PlayerCode;

            SaveStateChunkConsts consts;

            reader.Skip(1);

            consts.Version = header.Version;
            m_CurrentConsts = consts;

            SaveScratchpad scratch;
            scratch.Allocator = m_ScratchBuffer;
            m_ScratchBuffer.Reset();
            scratch.BlockCount = 0;
            scratch.Blocks = m_ScratchBuffer.AllocSpan<SaveScratchBlock>(32);
            m_CurrentScratch = scratch;

            SaveStateManifest manifest = reader.Read<SaveStateManifest>();
            reader.Skip(manifest.Padding);

            if (manifest.Length != reader.Remaining) {
                Log.Error("[SaveMgr] Mismatch between read bytes ({0}) and manifest bytes ({1})", reader.Remaining, manifest.Length);
                return false;
            }

            ulong calculatedChecksum = Unsafe.Hash64(reader.Head, reader.Remaining);
            Log.Msg("[SaveMgr] Calculated checksum from <{0},{1}> = {2}", reader.Head - m_MainBuffer, reader.Remaining, calculatedChecksum);
            ;
            if (calculatedChecksum != manifest.Checksum) {
                Log.Warn("[SaveMgr] Checksum failed - calculated {0} but expected {1}", calculatedChecksum, manifest.Checksum);
                //return false;
            }

            Log.Msg("[SaveMgr] Reading save data ({0} chunks, {1})", manifest.ChunkCount, Unsafe.FormatBytes(m_UsedSize));

            m_ChunkRecords.Clear();

            int chunkCount = (int) manifest.ChunkCount;
            while(chunkCount-- > 0) {
                SaveStateChunkHeader chunkHeader = reader.Read<SaveStateChunkHeader>();
                m_ChunkRecords.PushBack(new ChunkRecord() {
                    Id = chunkHeader.Id,
                    UncompressedSize = chunkHeader.ChunkLengthUncompressed,
                    Data = new UnsafeSpan<byte>(reader.Head, chunkHeader.ChunkLength)
                });
                Log.Msg("[SaveMgr] Read chunk '{0}' ({1}, {2} uncompressed)", chunkHeader.Id, Unsafe.FormatBytes(chunkHeader.ChunkLength), Unsafe.FormatBytes(chunkHeader.ChunkLengthUncompressed));
                //Log.Msg("...compressed " + Unsafe.DumpMemory(reader.Head, chunkHeader.ChunkLength, ' ', 2));
                reader.Skip((int) chunkHeader.ChunkLength);
            }

            Assert.True(reader.Remaining == 0);

            if (!string.IsNullOrEmpty(header.PlayerCode)) {
                PlayerPrefs.SetString("LatestPlayerCode", header.PlayerCode);
                PlayerPrefs.Save();
            }

            return true;
        }

        public void HandleChunks() {
            foreach (var chunk in m_ChunkRecords) {
                if (m_ChunkReaders.TryGetValue(chunk.Id, out ChunkReader reader)) {
                    ByteReader bytes = UnpackChunkRecord(chunk);
                    reader.Reader(reader.Context, ref bytes, m_CurrentConsts, ref m_CurrentScratch);
                    Assert.True(bytes.Remaining == 0);
                    ReleaseChunk(bytes);
                }
            }
        }

        public void HandlePostLoad() {
            foreach (var postLoad in m_PostLoadHandlers) {
                postLoad.PostLoad(m_CurrentConsts, ref m_CurrentScratch);
            }
        }

        private unsafe ByteReader UnpackChunkRecord(ChunkRecord record) {
            if (m_ActiveChunk != record.Id) {
                if (!m_ActiveChunk.IsEmpty) {
                    Log.Warn("[SaveMgr] Active chunk changing from '{0}' to '{1}'", m_ActiveChunk, record.Id);
                } else {
                    Log.Msg("[SaveMgr] Active chunk '{0}' ", record.Id);
                }
            }

            m_ActiveChunk = record.Id;
            if (record.UncompressedSize == record.Data.Length) {
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(record.Data.Ptr, record.Data.Length, ' ', 2));
                return new ByteReader() {
                    Head = record.Data.Ptr,
                    Remaining = record.Data.Length,
                };
            } else {
                // TODO: decompress if desired
                /*
                int decompressedSize;
                bool decompressed = UnsafeExt.Decompress(record.Data.Ptr, record.Data.Length, m_ChunkBuffer, ChunkBufferSize, &decompressedSize);

                Assert.True(decompressed && record.UncompressedSize == decompressedSize);
                //Log.Msg("...uncompressed " + Unsafe.DumpMemory(m_ChunkBuffer, decompressedSize, ' ', 2));
                */
                return new ByteReader() {
                    Head = m_ChunkBuffer,
                    Remaining = (int) record.UncompressedSize,
                };
            }
        }

        public void ReleaseChunk(ByteReader reader) {
            m_ActiveChunk = default;
        }

        #endregion // Read

        #region Chars

        public unsafe UnsafeSpan<char> GetCurrentBase64() {
            return new UnsafeSpan<char>(m_CharBuffer, (uint) m_CharsWritten);
        }

        public unsafe string GetCurrentBase64AsString() {
            return new string(m_CharBuffer, 0, m_CharsWritten);
        }

        public unsafe UnsafeSpan<char> EncodeToBase64() {
            var asSysSpan = new ReadOnlySpan<byte>(m_MainBuffer, m_UsedSize);
            bool converted = Convert.TryToBase64Chars(asSysSpan, new Span<char>(m_CharBuffer, CharBufferSize), out int charsWritten);
            Assert.True(converted);
            m_CharsWritten = charsWritten;
            return new UnsafeSpan<char>(m_CharBuffer, (uint) charsWritten);
        }

        public unsafe bool DecodeFromBase64(string str) {
            bool converted = Convert.TryFromBase64String(str, new Span<byte>(m_MainBuffer, MainBufferSize), out m_UsedSize);
            //Assert.True(converted);
            return converted;
        }

        #endregion // Chars
    }

    public enum SaveSlot {
        Main,
        Uncommitted
    }
}