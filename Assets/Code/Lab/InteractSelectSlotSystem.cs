using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 500)] // After Interactable Select System
    public class InteractSelectSlotSystem : ComponentSystemBehaviour<LabInteractable, InteractSelectSlot>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, InteractSelectSlot secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }

            var transferState = Find.State<DataTransferState>();
            DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
        }
    }
}