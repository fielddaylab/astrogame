using BeauUtil;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace FieldDay.Vox {
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct VoxRequestHandle : IEquatable<VoxRequestHandle> {
        [FieldOffset(0)] internal readonly UniqueId16 m_Id;

        internal VoxRequestHandle(UniqueId16 id) {
            m_Id = id;
        }

        public bool IsValid {
            get { return m_Id.Id != 0; }
        }

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is VoxRequestHandle) {
                return Equals((VoxRequestHandle) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            return m_Id.GetHashCode();
        }

        public bool Equals(VoxRequestHandle other) {
            return m_Id == other.m_Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator ==(VoxRequestHandle a, VoxRequestHandle b) {
            return a.m_Id == b.m_Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator !=(VoxRequestHandle a, VoxRequestHandle b) {
            return a.m_Id != b.m_Id;
        }

        #endregion // Overrides

        static public readonly VoxRequestHandle Dummy = new VoxRequestHandle(new UniqueId16(ushort.MaxValue));
    }
}