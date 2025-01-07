using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000)] // After InteractSelectSlotSystem
    public class SlotEffectSystem : ComponentSystemBehaviour<DataSlot, RelevantSlotHighlight>
    {
        public override void ProcessWorkForComponent(DataSlot component, RelevantSlotHighlight highlight, float deltaTime)
        {
            var transferState = Find.State<DataTransferState>();
            if (!transferState.SourceUpdated) { return; }
            if (!transferState.SelectedSource.IsSource) { return; }

            var highlightState = Find.State<SlotHighlightState>();
            bool isHighlightedAvailable =
                (transferState.SelectedTarget == null)
                && (component.Type & transferState.SelectedSource.Type) != 0
                && component.Modifiable;
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
