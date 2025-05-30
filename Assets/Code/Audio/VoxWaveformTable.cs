using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Audio {
    [CreateAssetMenu(menuName = "Astro/Vox Waveform Table")]
    public sealed class VoxWaveformTable : NamedAsset, IByteReadable, IRegistrationCallbacks {
        [SerializeField, EditModeOnly, Range(512, 2048)] private int m_EntryCapacity = 512;
        [SerializeField, EditModeOnly, Range(16, 256)] private int m_MaxChunksKb = 128;

        public struct TOCEntry {
            public StringHash32 LineCode;
            public OffsetLengthU16 Chunks;
        }

        private Dictionary<StringHash32, OffsetLengthU16> m_TOC;
        private UnsafeSpan<VoxWaveformChunk> m_ChunkBuffer;

        public unsafe void ReadFrom(ref ByteReader reader) {
            uint entryCount = reader.Read<uint>();

            m_TOC.Clear();
            m_TOC.EnsureCapacity((int)entryCount);

            int maxChunkEnd = 0;

            while(entryCount-- > 0) {
                TOCEntry entry = reader.Read<TOCEntry>();
                m_TOC.Add(entry.LineCode, entry.Chunks);
                maxChunkEnd = Math.Max(maxChunkEnd, entry.Chunks.End);
            }

            uint totalChunks = reader.Read<uint>();
            Assert.True(totalChunks == maxChunkEnd);
            Assert.True(totalChunks <= m_ChunkBuffer.Length, "Ran out of space in VoxWaveformManifest chunk buffer ({0} vs {1}) - increase it!", totalChunks, m_ChunkBuffer.Length);

            reader.ReadBuffer(m_ChunkBuffer.Ptr, m_ChunkBuffer.Length);
        }

        unsafe void IRegistrationCallbacks.OnRegister() {
            m_TOC = MapUtils.Create<StringHash32, OffsetLengthU16>(m_EntryCapacity);
            int maxChunks = m_MaxChunksKb * Unsafe.KiB / VoxWaveform.BytesPerChunk;
            Assert.True(maxChunks <= ushort.MaxValue + 1);
            m_ChunkBuffer = Unsafe.AllocSpan<VoxWaveformChunk>(maxChunks);
            Log.Msg("[VoxWaveformTable] Allocated space for {0} chunks", m_ChunkBuffer.Length);
        }

        unsafe void IRegistrationCallbacks.OnDeregister() {
            Unsafe.Free(m_ChunkBuffer.Ptr);
        }
    }
}