using BeauUtil;
using System.Runtime.InteropServices;

namespace FieldDay.Localization {
    public struct LocField {
        private NonBoxedValue m_Value;
    }

    public enum LocFieldType : byte {
        Null,
        Bool,
        Byte,
        SByte,
        Char,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        LocId,
        String
    }
}