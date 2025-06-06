using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauRoutine;
using FieldDay.Scripting;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 510, AstroGame.PuzzleSubmissionUpdateMask)] // After InteractSelectPuzzleCellSystem
    public class InteractSelectPuzzleCellSystem : ComponentSystemBehaviour<InteractSelectSlot, LabInteractable, InteractSelectPuzzleCell>
    {
        public override bool HasWork()
        {
            return base.HasWork() && (Find.State<ViewState>().ActiveNode?.AllowSlotSelection ?? false);
        }

        public override void ProcessWorkForComponent(InteractSelectSlot primary, LabInteractable secondary, InteractSelectPuzzleCell tertiary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }
            if (!primary.DataSlot.IsActive || primary.DataSlot.IsHidingData) { return; }

            using (var table = TempVarTable.Alloc())
            {
                table.Set("cellId", primary.DataSlot.SlotId);
                ScriptUtility.Trigger(ScriptEvents.OnPuzzleCellSelected, table);
            }
        }
    }
}