using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Localization {
    /// <summary>
    /// Three-character language code.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct LanguageId : IEquatable<LanguageId>, IComparable<LanguageId> {

        #region Data

        [FieldOffset(0), SerializeField] private byte m_0;
        [FieldOffset(1), SerializeField] private byte m_1;
        [FieldOffset(2), SerializeField] private byte m_2;
        [FieldOffset(0), NonSerialized] private uint m_Raw;

        #endregion // Data

        #region Constructors

        public LanguageId(string threeLetterCode) {
            threeLetterCode = threeLetterCode ?? string.Empty;
            m_Raw = 0;
            m_0 = threeLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(threeLetterCode[0]) : (byte) 0;
            m_1 = threeLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(threeLetterCode[1]) : (byte) 0;
            m_2 = threeLetterCode.Length > 2 ? (byte) char.ToLowerInvariant(threeLetterCode[2]) : (byte) 0;
        }

        public LanguageId(StringSlice threeLetterCode) {
            m_Raw = 0;
            m_0 = threeLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(threeLetterCode[0]) : (byte) 0;
            m_1 = threeLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(threeLetterCode[1]) : (byte) 0;
            m_2 = threeLetterCode.Length > 2 ? (byte) char.ToLowerInvariant(threeLetterCode[2]) : (byte) 0;
        }

        public LanguageId(CultureInfo info)
            : this(info?.ThreeLetterISOLanguageName) {
        }

        public LanguageId(uint value) {
            m_0 = 0;
            m_1 = 0;
            m_2 = 0;
            m_Raw = value;
        }

        #endregion // Constructors

        public bool IsEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Raw == 0; }
        }

        public uint Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Raw; }
        }

        #region Interfaces

        public bool Equals(LanguageId other) {
            return m_Raw == other.m_Raw;
        }

        public int CompareTo(LanguageId other) {
            return m_Raw.CompareTo(other.m_Raw);
        }

        #endregion // Interfaces

        #region Overrides

        public override string ToString() {
            unsafe {
                char* buffer = stackalloc char[3];
                buffer[0] = m_0 == 0 ? ' ' : (char) m_0;
                buffer[1] = m_1 == 0 ? ' ' : (char) m_1;
                buffer[2] = m_2 == 0 ? ' ' : (char) m_2;
                return new string(buffer, 0, 3);
            }
        }

        public override int GetHashCode() {
            return (int) m_Raw;
        }

        public override bool Equals(object obj) {
            if (obj is LanguageId) {
                return Equals((LanguageId) obj);
            } else {
                return false;
            }
        }

        #endregion // Overrides

        #region Operators

        static public bool operator ==(LanguageId left, LanguageId right) {
            return left.m_Raw == right.m_Raw;
        }

        static public bool operator !=(LanguageId left, LanguageId right) {
            return left.m_Raw != right.m_Raw;
        }

        #endregion // Operators

        static public readonly LanguageId English = new LanguageId("eng");
        static public readonly LanguageId Spanish = new LanguageId("spa");
        static public readonly LanguageId French = new LanguageId("fra");
        static public readonly LanguageId German = new LanguageId("ger");
        static public readonly LanguageId Italian = new LanguageId("ita");
        static public readonly LanguageId Dutch = new LanguageId("dut");
        static public readonly LanguageId Japanese = new LanguageId("jpn");

        /// <summary>
        /// Identifies a three-letter language code in a file path.
        /// File name should be of the format "fileName.code.strg" (ex. "mainText.eng.strg")
        /// </summary>
        static public LanguageId IdentifyLanguageFromPath(string filePath) {
            StringSlice pathWithoutExt;
            if (filePath.EndsWith(LocFile.FileExtensionWithDot)) {
                pathWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            } else {
                pathWithoutExt = Path.GetFileName(filePath);
            }

            if (pathWithoutExt.Length > 4 && pathWithoutExt[pathWithoutExt.Length - 4] == '.') {
                StringSlice langCode = pathWithoutExt.Substring(pathWithoutExt.Length - 3);
                return new LanguageId(langCode);
            } else {
                return default(LanguageId);
            }
        }
    }
}