using System;

namespace Astro {
    [Flags]
    public enum SpectrographMaterialMask {
        Hydrogen = 0x001,
        Helium = 0x002,
        Carbon = 0x004,
        Iron = 0x008,
        Calcium = 0x010,
        Sodium = 0x020,
        Magnesium = 0x040,
        Oxygen = 0x080,
        Titanium = 0x100,
    }
}