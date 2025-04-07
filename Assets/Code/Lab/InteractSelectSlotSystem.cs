using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauRoutine;

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

            // If nothing selected, 
            if (transferState.SelectedSource == null && transferState.SelectedTarget == null) {
                // and component can be a source, 
                if (secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
                } else {
                    DataUtility.AssignSelectedTarget(transferState, secondary.DataSlot);
                }
            }
            // Some source is selected (but no target)
            else if (transferState.SelectedSource != null)
            {
                // If source type matches this OR  source sibling exists and its type matches this 
                if (((transferState.SelectedSource.Type & secondary.DataSlot.Type) != 0 
                        || (transferState.SelectedSource.SiblingSlot != null 
                            && (transferState.SelectedSource.SiblingSlot.Type & secondary.DataSlot.Type) != 0))
                    // AND source or sibling is not itself the target
                    && (!transferState.SelectedSource.Equals(secondary.DataSlot) || (transferState.SelectedSource.SiblingSlot != null && !transferState.SelectedSource.SiblingSlot.Equals(secondary.DataSlot)))
                    // AND target is modifiable
                    && secondary.DataSlot.Modifiable
                    && !secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedTarget(transferState, secondary.DataSlot);
                }
                // Else selected is not a valid target. If it is a valid source, set as current source
                else if (secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, secondary.DataSlot);
                }
            }
            // Some target is selected (but no source)
            else if (transferState.SelectedTarget != null) {
                // if the target type matches this OR this sibling exists and its type matches target
                if (((transferState.SelectedTarget.Type & secondary.DataSlot.Type) != 0 || (secondary.DataSlot.SiblingSlot != null && (transferState.SelectedTarget.Type & secondary.DataSlot.SiblingSlot.Type) != 0))
                    // AND SelectedTarget is not itself the clicked source
                    && (!transferState.SelectedTarget.Equals(secondary.DataSlot))
                    && secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, secondary.DataSlot, true);

                } else if (!secondary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedTarget(transferState, secondary.DataSlot);
                }
            }
        }
    }
}