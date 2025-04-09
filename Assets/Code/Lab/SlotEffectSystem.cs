using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 2000, AstroGame.SubmissionUpdateMask)] // After InteractSelectSlotSystem
    public class SlotEffectSystem : ComponentSystemBehaviour<DataSlot, RelevantSlotHighlight>
    {
        public override void ProcessWorkForComponent(DataSlot component, RelevantSlotHighlight highlight, float deltaTime)
        {
            var transferState = Find.State<DataTransferState>();
            if (!transferState.SourceUpdated) { return; }
            if (transferState.SelectedSource != null && !transferState.SelectedSource.IsSource) { return; }

            var highlightState = Find.State<SlotHighlightState>();

            bool sourceNotNull = transferState.SelectedSource != null;
            bool targetIsNull = transferState.SelectedTarget == null;
            bool hasSibling = sourceNotNull && transferState.SelectedSource.SiblingSlot != null;
            bool typesMatch = sourceNotNull && ((component.Type & transferState.SelectedSource.Type) != 0 || (hasSibling && (component.Type & transferState.SelectedSource.SiblingSlot.Type) != 0));
            bool slotIsModifiable = component.Modifiable;

            bool isHighlightedAvailable = sourceNotNull && targetIsNull && typesMatch && slotIsModifiable;
            bool isHighlightedSelected = component.Equals(transferState.SelectedTarget);

            if (isHighlightedSelected) {
                SlotHighlightUtility.SetSelectedHighlight(highlightState, highlight, true);
            } 
            else if (isHighlightedAvailable) {
                SlotHighlightUtility.SetAvailableHighlight(highlightState, highlight, true);
            }
            else {
                SlotHighlightUtility.SetSelectedHighlight(highlightState, highlight, false);
            }
        }
    }
}
