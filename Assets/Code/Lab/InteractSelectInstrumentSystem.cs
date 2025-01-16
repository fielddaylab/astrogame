using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 750)] // After Interactable Select System
    public class InteractSelectInstrumentSystem : ComponentSystemBehaviour<LabInteractable, InteractSelectInstrument>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, InteractSelectInstrument secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }
            if (primary.transform.parent.TryGetComponent(out LabInstrument instrument) && !instrument.Unlocked) {
                if (instrument.PointsToUnlock <= PointsUtility.GetPoints()) {
                    InstrumentInventoryUtility.SetInstrumentUnlocked(instrument, true);
                    DataDistributionUtility.QueueConversion(Find.State<DataPacketDistributionState>(), Find.State<FocusState>().CurrentFocus.TargetData);
                }
                return;
            }
        }
    }
}