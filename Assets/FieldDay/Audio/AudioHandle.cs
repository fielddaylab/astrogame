using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio playback handle.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct AudioHandle : IEquatable<AudioHandle> {
        [FieldOffset(0)] internal readonly UniqueId16 m_Id;

        internal AudioHandle(UniqueId16 id) {
            m_Id = id;
        }

        public bool IsValid {
            get { return m_Id.Id != 0; }
        }

        #region Overrides

        public override bool Equals(object obj) {
            if (obj is AudioHandle) {
                return Equals((AudioHandle) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            return m_Id.GetHashCode();
        }

        public bool Equals(AudioHandle other) {
            return m_Id == other.m_Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator==(AudioHandle a, AudioHandle b) {
            return a.m_Id == b.m_Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool operator !=(AudioHandle a, AudioHandle b) {
            return a.m_Id != b.m_Id;
        }

        #endregion // Overrides
    }
}