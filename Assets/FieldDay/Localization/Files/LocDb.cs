using System.Collections.Generic;
using System.Runtime.CompilerServices;
using FieldDay.Data;

namespace FieldDay.Localization {
    /// <summary>
    /// Localization database.
    /// </summary>
    public sealed class LocDb : IByteSerializable
    {
        private readonly Dictionary<uint, string> m_Entries;
        private readonly HashSet<uint> m_EntriesWithTags;

        public LocDb(int capacity) {
            m_Entries = new Dictionary<uint, string>(capacity);
            m_EntriesWithTags = new HashSet<uint>(capacity / 4);
        }

        /// <summary>
        /// Attempts to retrieve the entry for the given localization id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryFind(LocId id, out string text) {
            return m_Entries.TryGetValue(id.HashValue, out text);
        }

        /// <summary>
        /// Retrieves the entry for the given localization id.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Find(LocId id) {
            string text;
            m_Entries.TryGetValue(id.HashValue, out text);
            return text;
        }

        /// <summary>
        /// Returns the number of keys present.
        /// </summary>
        public int Count {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Entries.Count; }
        }

        /// <summary>
        /// Clears the database.
        /// </summary>
        public void Clear() {
            m_Entries.Clear();
            m_EntriesWithTags.Clear();
        }

        #region IByteSerializable

        public void ReadFrom(ref ByteReader reader) {
            uint entryCount = reader.Read<uint>();
            m_Entries.Clear();
            m_Entries.EnsureCapacity((int) entryCount);

            while(entryCount-- > 0) {
                uint hash = reader.Read<uint>();
                string val = reader.ReadUTF8();
                m_Entries.Add(hash, val);
            }

            uint taggedEntryCount = reader.Read<uint>();
            m_EntriesWithTags.Clear();
            m_EntriesWithTags.EnsureCapacity((int) taggedEntryCount);

            while(taggedEntryCount-- > 0) {
                uint hash = reader.Read<uint>();
                m_EntriesWithTags.Add(hash);
            }
        }

        public void WriteTo(ref ByteWriter writer) {
            writer.Write((uint) m_Entries.Count);
            
            foreach(var entry in m_Entries) {
                writer.Write(entry.Key);
                writer.WriteUTF8(entry.Value);
            }

            writer.Write((uint)m_EntriesWithTags.Count);
            foreach(var taggedEntry in m_EntriesWithTags) {
                writer.Write(taggedEntry);
            }
        }

        #endregion // IByteSerializable
    }
}