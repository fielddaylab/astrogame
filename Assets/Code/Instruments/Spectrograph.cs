using System;

namespace Astro {
    [Flags]
    public enum SpectrographMaterialMask {
        Hydrogen = 0x01,
        Helium = 0x02,
        Carbon = 0x04,
        Iron = 0x08,
        Calcium = 0x10,
        Sodium = 0x20,
        Magnesium = 0x40,
        Oxygen = 0x80
    }
}