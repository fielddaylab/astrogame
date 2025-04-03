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
            var cancelInputState = Find.State<CancelInputState>();
            cancelInputState.SlotClicked = true;

            // If nothing selected, and component can be a source, set source
            if (transferState.SelectedSource == null && transferState.SelectedTarget == null && secondary.DataSlot.IsSource) {
                DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
            }
            // If some source is selected...
            else if (transferState.SelectedSource != null)
            {
                // If source is correct type OR sibling exists and is correct type
                if ((transferState.SelectedSource.Type & secondary.DataSlot.Type) != 0 || (transferState.SelectedSource.SiblingSlot != null && (transferState.SelectedSource.SiblingSlot.Type & secondary.DataSlot.Type) != 0)
                    // AND source or sibling is not itself the target
                    && (!transferState.SelectedSource.Equals(secondary.DataSlot) || !transferState.SelectedSource.SiblingSlot.Equals(secondary.DataSlot))
                    // AND target is modifiable
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