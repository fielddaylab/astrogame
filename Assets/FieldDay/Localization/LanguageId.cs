using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using UnityEngine;

namespace FieldDay.Localization {
    /// <summary>
    /// Two-character language code.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Explicit, Size = 2)]
    public struct LanguageId : IEquatable<LanguageId>, IComparable<LanguageId> {

        #region Data

        [FieldOffset(0), NonSerialized] private byte m_0;
        [FieldOffset(1), NonSerialized] private byte m_1;
        [FieldOffset(0), SerializeField] private ushort m_Raw;

        #endregion // Data

        #region Constructors

        public LanguageId(string twoLetterCode) {
            twoLetterCode = twoLetterCode ?? string.Empty;
            Assert.True(twoLetterCode.Length <= 2);
            m_Raw = 0;
            m_0 = twoLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(twoLetterCode[0]) : (byte) 0;
            m_1 = twoLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(twoLetterCode[1]) : (byte) 0;
        }

        public LanguageId(StringSlice twoLetterCode) {
            Assert.True(twoLetterCode.Length <= 2);
            m_Raw = 0;
            m_0 = twoLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(twoLetterCode[0]) : (byte) 0;
            m_1 = twoLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(twoLetterCode[1]) : (byte) 0;
        }

        public LanguageId(CultureInfo info)
            : this(info?.TwoLetterISOLanguageName) {
        }

        public LanguageId(ushort value) {
            m_0 = 0;
            m_1 = 0;
            m_Raw = value;
        }

        #endregion // Constructors

        public bool IsEmpty {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Raw == 0; }
        }

        public ushort Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return m_Raw; }
        }

        #region Interfaces

        public bool Equals(LanguageId other) {
            return m_Raw == other.m_Raw;
        }

        public int CompareTo(LanguageId other) {
            return m_Raw - other.m_Raw;
        }

        #endregion // Interfaces

        #region Overrides

        public override string ToString() {
            unsafe {
                char* buffer = stackalloc char[2];
                buffer[0] = m_0 == 0 ? ' ' : (char) m_0;
                buffer[1] = m_1 == 0 ? ' ' : (char) m_1;
                return new string(buffer, 0, 2);
            }
        }

        public override int GetHashCode() {
            return m_Raw;
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

        static public readonly LanguageId English = new LanguageId("en");
        static public readonly LanguageId Spanish = new LanguageId("es");
        static public readonly LanguageId French = new LanguageId("fr");
        static public readonly LanguageId German = new LanguageId("de");
        static public readonly LanguageId Italian = new LanguageId("it");
        static public readonly LanguageId Dutch = new LanguageId("nl");
        static public readonly LanguageId Japanese = new LanguageId("ja");

        /// <summary>
        /// Identifies a two-letter language code in a file path.
        /// File name should be of the format "fileName.code.ext" (ex. "mainText.es.ext")
        /// </summary>
        static public LanguageId IdentifyLanguageFromPath(string filePath, string expectedExtensionWithDot) {
            StringSlice pathWithoutExt;
            if (filePath.EndsWith(expectedExtensionWithDot)) {
                pathWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            } else {
                pathWithoutExt = Path.GetFileName(filePath);
            }

            if (pathWithoutExt.Length > 3 && pathWithoutExt[pathWithoutExt.Length - 3] == '.') {
                StringSlice langCode = pathWithoutExt.Substring(pathWithoutExt.Length - 2);
                return new LanguageId(langCode);
            } else {
                return default(LanguageId);
            }
        }
    }
}