using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    public class InteractSelectInstrumentSystem : ComponentSystemBehaviour<LabInteractable, InteractSelectInstrument>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, InteractSelectInstrument secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }
            if (primary.transform.parent.TryGetComponent(out LabInstrument instrument) && !instrument.Unlocked) {
                // TODO: try unlock instrument
                if (instrument.PointsToUnlock <= PointsUtility.GetPoints()) {
                    InstrumentInventoryUtility.SetInstrumentUnlocked(instrument, true);
                }
                return;
            }
            var puzzleState = Find.State<PuzzleState>();
            var transferState = Find.State<DataTransferState>();

            puzzleState.RelevantColFilter = InstrumentUtility.GenerateTypeMask(secondary.Instrument);
            puzzleState.CellsUpdated = true;
        }
    }
}