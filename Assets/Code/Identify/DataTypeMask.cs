using System;
using FieldDay.Components;

namespace Astro {
    [Flags]
    public enum DataTypeMask : uint {
        Name = 0x001,
        Coordinates = 0x002,
        Color = 0x008,

        ApparentMagnitude = 0x010,
        AbsoluteMagnitude = 0x020,
        
        MaterialSpectrum = 0x040,
        
        Temperature = 0x080,
        Distance = 0x100,

        Historical_Coordinates = 0x200,
        Historical_ApparentMagnitude = 0x400,
        Historical_Temperature = 0x800,
        Historical_Distance = 0x1000,
        Historical_Color = 0x2000
    }
}