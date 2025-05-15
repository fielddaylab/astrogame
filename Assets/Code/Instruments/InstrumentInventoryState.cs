using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leaf.Runtime;
using FieldDay.Scripting;
using FieldDay.Scenes;
using BeauUtil.Debugger;
using FieldDay.Debugging;


namespace Astro {
    public class InstrumentInventoryState : SharedStateComponent {
        // General access to instruments
        public RingBuffer<LabInstrument> ActiveInstruments = new RingBuffer<LabInstrument>(8);

        // Quicker access structured on instrument data types
        public Dictionary<DataTypeMask, List<LabInstrument>> ActiveInstrumentMap = new Dictionary<DataTypeMask, List<LabInstrument>>();
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
                    //var progressState = Find.State<PlayerProgressState>();
                    //progressState.UnlockedInstruments.Add(actorId);
                }
                TryAddToActiveInstruments(instrument);
                instrument.OnUnlock?.Invoke(instrument);

                AstroGame.Events.Queue(GameEvents.InstrumentUnlocked, ScriptUtility.ActorId(instrument));
            }
        }

        private static void TryAddToActiveInstruments(LabInstrument instrument, InstrumentInventoryState inventory = null) {
            if (inventory == null) {
                inventory = Find.State<InstrumentInventoryState>();
            }
            if (inventory.ActiveInstruments.Contains(instrument)) return; 

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

#if DEVELOPMENT

        [DebugMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Instruments", 6);
            AddInstrumentUnlockButton(menu, "PhotometerInstrument");
            AddInstrumentUnlockButton(menu, "BlueWavelength");
            AddInstrumentUnlockButton(menu, "InfraredWavelength");
            AddInstrumentUnlockButton(menu, "ColorInstrument");
            AddInstrumentUnlockButton(menu, "TemperatureInstrument");
            AddInstrumentUnlockButton(menu, "SpectrometerInstrument");
            AddInstrumentUnlockButton(menu, "HistoricalDataInstrument");
            return menu;
        }

        static private void AddInstrumentUnlockButton(DMInfo info, string instrumentName) {
            info.AddButton("Unlock " + instrumentName, () => {
                ScriptActor actor = ScriptUtility.FindActor(instrumentName);
                if (actor != null) {
                    SetInstrumentUnlocked(actor.GetComponent<LabInstrument>(), true, true, actor.Id);
                }
            });
        }

#endif // DEVELOPMENT

    }
}