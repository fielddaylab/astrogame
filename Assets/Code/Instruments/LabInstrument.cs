using System;
using System.Collections.Generic;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    public sealed class LabInstrument : BatchedComponent {
        public DataSlot[] AutoPopulated;
        
        [NonSerialized] public bool Unlocked;
        public CastableEvent<LabInstrument> OnUnlock = new CastableEvent<LabInstrument>();
    }

    public static partial class InstrumentUtility
    {
        public static DataTypeMask GenerateTypeMask(LabInstrument instrument)
        {
            DataTypeMask allTypes = 0;
            foreach (var slot in instrument.AutoPopulated)
            {
                allTypes |= slot.Type;
            }

            return allTypes;
        }
    }
}