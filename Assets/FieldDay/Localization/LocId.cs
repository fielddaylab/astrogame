using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Localization {
    /// <summary>
    /// Localization key.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToDebugString()}")]
    public struct LocId : IDebugString, IEquatable<LocId>, IComparable<LocId>
    {
        [SerializeField] private StringHash32 m_HashValue;

        public LocId(StringHash32 hash) {
            m_HashValue = hash;
        }

        public LocId(StringSlice source) {
            m_HashValue = new StringHash32(source);
        }

        public LocId(string source) {
            m_HashValue = new StringHash32(source);
        }

        #region Interfaces

        public int CompareTo(LocId other) {
            return m_HashValue.CompareTo(other.m_HashValue);
        }

        public bool Equals(LocId other) {
            return m_HashValue.Equals(other.m_HashValue);
        }

        public string ToDebugString() {
            return m_HashValue.ToDebugString();
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is LocId)
                return Equals((LocId)obj);

            return false;
        }

        public override int GetHashCode() {
            return unchecked((int)m_HashValue.HashValue);
        }

        public override string ToString() {
            return string.Format("@{0:X8}", m_HashValue.HashValue);
        }

        #endregion // Overrides

        #region Operators

        static public bool operator ==(LocId left, LocId right) {
            return left.m_HashValue == right.m_HashValue;
        }

        static public bool operator !=(LocId left, LocId right) {
            return left.m_HashValue != right.m_HashValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public implicit operator LocId(StringHash32 inHash) {
            return new LocId(inHash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public implicit operator LocId(StringSlice inSlice) {
            return new LocId(inSlice);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public implicit operator LocId(string inString) {
            return new StringHash32(inString);
        }

        static public explicit operator bool(LocId inHash) {
            return inHash.m_HashValue != 0;
        }

        #endregion // Operators
    }
}