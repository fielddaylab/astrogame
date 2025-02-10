using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leaf.Runtime;
using FieldDay.Scripting;


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
            //instrument.UnlockText.SetText(instrument.PointsToUnlock.ToStringLookup());

            SetInstrumentUnlocked(instrument, true);
        }

        [LeafMember("SetInstrumentUnlocked")]
        private static void LeafSetInstrumentUnlocked(ScriptActor actor, bool unlocked) {
            LabInstrument instrument = actor.GetComponent<LabInstrument>();
            SetInstrumentUnlocked(instrument, unlocked);
        }

        public static void SetInstrumentUnlocked(LabInstrument instrument, bool unlocked) {
            //instrument.Unlocked = unlocked;
            //instrument.LockPanel.SetActive(!unlocked);
            if (unlocked) {
                AddToActiveInstruments(instrument);
            }
        }

        private static void AddToActiveInstruments(LabInstrument instrument, InstrumentInventoryState inventory = null) {
            if (inventory == null) {
                inventory = Find.State<InstrumentInventoryState>();
            }
            inventory.ActiveInstruments.PushBack(instrument);

            var currMap = inventory.ActiveInstrumentMap;
            foreach (DataSlot slot in instrument.AutoPopulated) {
                if (!currMap.ContainsKey(slot.Type)) {
                    currMap.Add(slot.Type, new List<LabInstrument>() { instrument });
                } else {
                    currMap[slot.Type].Add(instrument);
                }
            }
        }


    }
}