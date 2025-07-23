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
        public RingBuffer<LabInstrument> ActiveInstruments = new RingBuffer<LabInstrument>(10);

        // Quicker access structured on instrument data types
        public Dictionary<DataTypeMask, List<LabInstrument>> ActiveInstrumentMap = new Dictionary<DataTypeMask, List<LabInstrument>>();
    }

    public static class InstrumentInventoryUtility
    {
        [LeafMember("SetInstrumentUnlocked")]
        private static void LeafSetInstrumentUnlocked(ScriptActor actor, bool unlocked) {
            LabInstrument instrument = actor.GetComponent<LabInstrument>();
            SetInstrumentUnlocked(instrument, unlocked, actor.Id);
        }

        public static void SetInstrumentUnlocked(LabInstrument instrument, bool unlocked, StringHash32 actorId) {
            // TODO why did we do this? Did we want to make this lock at some point?
            if (!unlocked) return;

            // Check if instrument already in active instruments
            if (!TryAddToActiveInstruments(instrument)) return;

            instrument.OnUnlock?.Invoke(instrument);
            AstroGame.Events.Queue(GameEvents.InstrumentUnlocked, ScriptUtility.ActorId(instrument));
        }

        private static bool TryAddToActiveInstruments(LabInstrument instrument, InstrumentInventoryState inventory = null) {
            if (inventory == null) {
                inventory = Find.State<InstrumentInventoryState>();
            }

            if (inventory.ActiveInstruments.Contains(instrument))
                return false;

            instrument.Unlocked = true;
            inventory.ActiveInstruments.PushBack(instrument);

            var currMap = inventory.ActiveInstrumentMap;
            foreach (DataSlot slot in instrument.AutoPopulated) {
                if (!currMap.ContainsKey(slot.Type)) {
                    currMap.Add(slot.Type, new List<LabInstrument>() { instrument });
                } else {
                    currMap[slot.Type].Add(instrument);
                }
            }

            return true;
        }

#if DEVELOPMENT

        [DebugMenuFactory]
        static private DMInfo CreateDebugMenu() {
            DMInfo menu = new DMInfo("Instruments", 8);
            AddInstrumentUnlockButton(menu, "PhotometerInstrument");
            AddInstrumentUnlockButton(menu, "BlueWavelength");
            AddInstrumentUnlockButton(menu, "InfraredWavelength");
            AddInstrumentUnlockButton(menu, "ColorInstrument");
            AddInstrumentUnlockButton(menu, "TemperatureInstrument");
            AddInstrumentUnlockButton(menu, "SpectrometerInstrument");
            AddInstrumentUnlockButton(menu, "HistoricalDataInstrument");
            AddInstrumentUnlockButton(menu, "HistoricalDataModeToggle");
            AddDecoderUnlockButton(menu, "SatelliteDecoder");
            return menu;
        }

        static private void AddInstrumentUnlockButton(DMInfo info, string instrumentName) {
            info.AddButton("Unlock " + ReflectionCache.InspectorName(instrumentName), () => {
                ScriptActor actor = ScriptUtility.FindActor(instrumentName);
                if (actor != null) {
                    SetInstrumentUnlocked(actor.GetComponent<LabInstrument>(), true, actor.Id);
                }
            });
        }

        static private void AddDecoderUnlockButton(DMInfo info, string decoderName)
        {
            info.AddButton("Unlock " + decoderName, () => {
                ScriptActor actor = ScriptUtility.FindActor(decoderName);
                if (actor != null) {
                    actor.GetComponent<SatelliteDecoder>().SetDecoderActive(true);
                }
            });
        }

#endif // DEVELOPMENT

    }
}