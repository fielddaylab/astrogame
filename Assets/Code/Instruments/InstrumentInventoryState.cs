using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InstrumentInventoryState : SharedStateComponent
    {
        // General access to instruments
        public RingBuffer<LabInstrument> ActiveInstruments = new RingBuffer<LabInstrument>(8);

        // Quicker access structured on instrument data types
        public Dictionary<DataTypeMask, List<LabInstrument>> ActiveInstrumentMap = new Dictionary<DataTypeMask, List<LabInstrument>>();
    }

    public static class InstrumentInventoryUtility
    {
        public static void RegisterInstrument(LabInstrument instrument)
        {
            Find.State<InstrumentInventoryState>().ActiveInstruments.PushBack(instrument);

            var currMap = Find.State<InstrumentInventoryState>().ActiveInstrumentMap;
            foreach (DataSlot slot in instrument.AutoPopulated) {
                if (!currMap.ContainsKey(slot.Type)) {
                    currMap.Add(slot.Type, new List<LabInstrument>() { instrument });
                }
                else {
                    currMap[slot.Type].Add(instrument);
                }
            }
        }
    }
}