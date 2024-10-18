using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;

namespace FieldDay.Animation {
    /// <summary>
    /// Animation handle.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct AnimHandle : IEquatable<AnimHandle> {
        [FieldOffset(0)] private readonly uint m_Data;

        [FieldOffset(0)] private readonly UniqueId16 m_Handle;

        // (u) update phase
        // (t) anim type
        // uuuu uttt tt-- ----
        [FieldOffset(2)] private readonly ushort m_PackedAdditionalData;

        private const int UpdatePhaseShift = 0;
        private const int UpdatePhaseBits = 5;
        private const uint UpdatePhaseMask = (1 << UpdatePhaseBits) - 1;

        private const int AnimTypeShift = 5;
        private const int AnimTypeBits = 5;
        private const uint AnimTypeMask = (1 << AnimTypeBits) - 1;

        internal AnimHandle(UniqueId16 id, GameLoopPhase phase, AnimationType type) {
            m_Data = default;
            m_Handle = id;
            m_PackedAdditionalData = Pack(phase, type);
        }

        #region Properties

        internal UniqueId16 Id {
            get { return m_Handle; }
        }

        internal GameLoopPhase UpdatePhase {
            get { return (GameLoopPhase) ((m_PackedAdditionalData >> UpdatePhaseShift) & UpdatePhaseMask); }
        }

        internal AnimationType AnimType {
            get { return (AnimationType) ((m_PackedAdditionalData >> AnimTypeShift) & AnimTypeMask); }
        }

        #endregion // Properties

        #region Overrides

        public bool Equals(AnimHandle other) {
            return m_Data == other.m_Data;
        }

        public override bool Equals(object obj) {
            if (obj is AnimHandle) {
                return Equals((AnimHandle) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            return (int) m_Data;
        }

        public override string ToString() {
            if (m_Data == 0) {
                return "[null]";
            }

            return string.Format("[{0}, {1}, {2}]", m_Handle.ToString(), AnimType.ToString(), UpdatePhase.ToString());
        }

        #endregion // Overrides

        #region Operators

        static public bool operator ==(AnimHandle lhs, AnimHandle rhs) {
            return lhs.m_Data == rhs.m_Data;
        }

        static public bool operator !=(AnimHandle lhs, AnimHandle rhs) {
            return lhs.m_Data != rhs.m_Data;
        }

        static public implicit operator bool(AnimHandle handle) {
            return handle.m_Handle.Id != 0;
        }

        #endregion // Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static private ushort Pack(GameLoopPhase phase, AnimationType type) {
            return (ushort) (
                (((int) phase & UpdatePhaseMask) << UpdatePhaseShift)
                | (((int) type & AnimTypeMask) << AnimTypeShift)
                );
        }
    }

    public enum AnimationType : byte {
        Lite,
        Animator
    }
}