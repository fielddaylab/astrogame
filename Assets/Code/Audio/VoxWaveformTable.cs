using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Data;
using FieldDay.Files;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Audio {
    [CreateAssetMenu(menuName = "Astro/Vox Waveform Table")]
    public sealed class VoxWaveformTable : NamedAsset, IByteReadable, IRegistrationCallbacks {
        [SerializeField, EditModeOnly, Range(512, 2048)] private int m_EntryCapacity = 512;
        [SerializeField, EditModeOnly, Range(16, 256)] private int m_MaxChunksKb = 128;
        [SerializeField, AssetName(typeof(VoxWaveformTable))] private StringHash32 m_FallbackId;
        [Space]
        [SerializeField, StreamingPath] private string m_Path;

        public struct TOCEntry {
            public StringHash32 LineCode;
            public OffsetLengthU16 Chunks;
        }

        private Dictionary<StringHash32, OffsetLengthU16> m_TOC;
        private UnsafeSpan<VoxWaveformChunk> m_ChunkBuffer;
        [NonSerialized] private VoxWaveformTable m_CachedFallback;

        public bool TryFind(StringHash32 lineCode, out VoxWaveform waveform) {
            if (m_TOC.TryGetValue(lineCode, out var span)) {
                waveform.Chunks = m_ChunkBuffer.Slice(span.Offset, span.Length);
                return true;
            } else if (!m_FallbackId.IsEmpty) {
                Assert.True(m_FallbackId != AssetId, "Potential infinite loop");
                if (ReferenceEquals(m_CachedFallback, null)) {
                    m_CachedFallback = Find.NamedAsset<VoxWaveformTable>(m_FallbackId);
                }
                return m_CachedFallback.TryFind(lineCode, out waveform);
            } else {
                waveform.Chunks = default;
                return false;
            }
        }

        #region IByteReadable

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

            reader.ReadBuffer(m_ChunkBuffer.Ptr, (int) totalChunks);
        }

        #endregion // IByteReadable

        #region IRegistrationCallbacks

        unsafe void IRegistrationCallbacks.OnRegister() {
            m_TOC = MapUtils.Create<StringHash32, OffsetLengthU16>(m_EntryCapacity);
            int maxChunks = m_MaxChunksKb * Unsafe.KiB / VoxWaveform.BytesPerChunk;
            Assert.True(maxChunks <= ushort.MaxValue + 1);
            m_ChunkBuffer = Unsafe.AllocSpan<VoxWaveformChunk>(maxChunks);
            Log.Msg("[VoxWaveformTable] Allocated space for {0} chunks", m_ChunkBuffer.Length);

            FileLoadRequest readRequest;
            readRequest.Location = FileLocation.Streaming;
            readRequest.Path = m_Path;
            readRequest.Mode = FileBufferMode.Buffer;
            readRequest.Flags = 0;
            readRequest.Callback = HandleResult;
            readRequest.CallbackContext = this;
            Game.Files.RequestFile(readRequest, FileLoadPriority.High);
        }

        unsafe void IRegistrationCallbacks.OnDeregister() {
            Unsafe.Free(m_ChunkBuffer.Ptr);
            m_CachedFallback = null;
        }

        #endregion // IRegistrationCallbacks

        static private void HandleResult(FileLoadRequest request, FileLoadResult result, object context) {
            ByteReader reader = result.CreateByteReader();
            ((IByteReadable) context).ReadFrom(ref reader);
        }

        static public StringHash32 GenerateKey(string streamPath) {
            StringSlice waveformKeySlice = streamPath;
            int sharedIdx = waveformKeySlice.IndexOf("shared/");
            if (sharedIdx >= 0) {
                waveformKeySlice = waveformKeySlice.Substring(sharedIdx + 7);
            }
            int enIdx = waveformKeySlice.IndexOf("en/");
            if (enIdx >= 0) {
                waveformKeySlice = waveformKeySlice.Substring(enIdx + 3);
            }
            int extIdx = waveformKeySlice.LastIndexOf('.');
            if (extIdx >= 0) {
                waveformKeySlice = waveformKeySlice.Substring(0, extIdx);
            }
            return StringHash32.Fast(waveformKeySlice);
        }
    }
}