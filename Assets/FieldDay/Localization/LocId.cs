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
        [SerializeField] private uint m_HashValue;

        public LocId(StringHash32 hash) {
            m_HashValue = hash.HashValue;
        }

        public LocId(StringSlice source) {
            m_HashValue = new StringHash32(source).HashValue;
        }

        public LocId(string source) {
            m_HashValue = new StringHash32(source).HashValue;
        }

        public uint HashValue {
            get { return m_HashValue; }
        }

        #region Interfaces

        public int CompareTo(LocId other) {
            return (int) ((long) m_HashValue - (long) other.m_HashValue);
        }

        public bool Equals(LocId other) {
            return m_HashValue == other.m_HashValue;
        }

        public string ToDebugString() {
            return new StringHash32(m_HashValue).ToDebugString();
        }

        #endregion // Interfaces

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is LocId)
                return Equals((LocId)obj);

            return false;
        }

        public override int GetHashCode() {
            return unchecked((int)m_HashValue);
        }

        public override string ToString() {
            return string.Format("@{0:X8}", m_HashValue);
        }

        #endregion // Overrides

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator ==(LocId left, LocId right) {
            return left.m_HashValue == right.m_HashValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public explicit operator bool(LocId inHash) {
            return inHash.m_HashValue != 0;
        }

        #endregion // Operators
    }
}