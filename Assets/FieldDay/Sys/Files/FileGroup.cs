using System.Collections.Generic;
using BeauData;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Files {
    public struct FileGroup {
        public readonly int Index;
        public readonly StringHash32[] Children;

        public readonly string[] Paths;
        public readonly StringHash32[] PathHashes;
    }

    public sealed class FileGroupManifest {
        private const int InitialCapacity = 16;

        private readonly Dictionary<StringHash32, FileGroup> m_Groups;
        private uint[] m_RefCounts;

        public FileGroupManifest() {
            m_Groups = new Dictionary<StringHash32, FileGroup>(InitialCapacity, CompareUtils.DefaultEquals<StringHash32>());
            m_RefCounts = new uint[InitialCapacity];
        }
    }
}