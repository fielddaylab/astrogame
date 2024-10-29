using System;
using System.Collections.Generic;
using FieldDay;
using FieldDay.Components;

namespace Astro {
    public sealed class LabInstrument : BatchedComponent, IRegistrationCallbacks {
        public DataSlot[] AutoPopulated;
        
        [NonSerialized] public bool Unlocked;

        public void OnDeregister() { }

        public void OnRegister() {
            // TEMP: register instruments on start
            InstrumentInventoryUtility.RegisterInstrument(this);
        }
    }

    public static class InstrumentUtility
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