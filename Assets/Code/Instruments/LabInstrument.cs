using System;
using FieldDay.Components;

namespace Astro {
    public sealed class LabInstrument : BatchedComponent {
        public DataSlot[] AutoPopulated;
        
        [NonSerialized] public bool Unlocked;
    }
}