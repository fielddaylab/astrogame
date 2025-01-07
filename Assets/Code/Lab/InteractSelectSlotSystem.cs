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

            // If nothing selected, and component can be a source, set source
            if (transferState.SelectedSource == null && transferState.SelectedTarget == null && secondary.DataSlot.IsSource) {
                DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
            }
            // If some source is selected...
            else if (transferState.SelectedSource != null)
            {
                // If target is valid
                if (((transferState.SelectedSource.Type & secondary.DataSlot.Type) != 0)
                    && !transferState.SelectedSource.Equals(secondary.DataSlot)
                    && secondary.DataSlot.Modifiable) {
                    DataUtility.AssignSelectedTarget(transferState, secondary.DataSlot);
                }
                // Else target is not valid. If a valid source, set as current source
                else if (secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
                }
            }
        }
    }
}