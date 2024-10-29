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

            var puzzleState = Find.State<PuzzleState>();
            puzzleState.RelevantColFilter = InstrumentUtility.GenerateTypeMask(secondary.Instrument);
            puzzleState.CellsUpdated = true;
        }

    }
}