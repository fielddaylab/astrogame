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

        public void Start()
        {
            // Load unlocked instruments from player progress
            var progressState = Find.State<PlayerProgressState>();
            foreach (var instrumentID in progressState.UnlockedInstruments)
            {
                var instrument = ScriptUtility.FindActor(instrumentID).GetComponent<LabInstrument>();
                InstrumentInventoryUtility.SetInstrumentUnlocked(instrument, true, false, "");
            }
        }
    }

    public static class InstrumentInventoryUtility
    {
        public static void RegisterInstrument(LabInstrument instrument) {

        }

        [LeafMember("SetInstrumentUnlocked")]
        private static void LeafSetInstrumentUnlocked(ScriptActor actor, bool unlocked) {
            LabInstrument instrument = actor.GetComponent<LabInstrument>();
            SetInstrumentUnlocked(instrument, unlocked, true, actor.Id);
        }

        public static void SetInstrumentUnlocked(LabInstrument instrument, bool unlocked, bool registerToProgress, StringHash32 actorId) {
            if (unlocked) {
                if (registerToProgress) {
                    var progressState = Find.State<PlayerProgressState>();
                    progressState.UnlockedInstruments.Add(actorId);
                }
                AddToActiveInstruments(instrument);
                instrument.OnUnlock?.Invoke(instrument);
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