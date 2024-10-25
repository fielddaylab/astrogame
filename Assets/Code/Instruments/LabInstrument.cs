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
}