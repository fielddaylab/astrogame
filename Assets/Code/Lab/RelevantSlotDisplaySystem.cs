using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 1000)] // After InteractSelectSlotSystem
    public class RelevantSlotDisplaySystem : ComponentSystemBehaviour<DataSlot, RelevantSlotHighlight>
    {
        public override void ProcessWorkForComponent(DataSlot component, RelevantSlotHighlight highlight, float deltaTime)
        {
            var transferState = Find.State<DataTransferState>();
            if (!transferState.SourceUpdated) { return; }
            if (!transferState.SelectedSource.IsSource) { return; }

            var highlightState = Find.State<SlotHighlightState>();
            bool isHighlighted = (transferState.SelectedTarget == null) && (component.Type & transferState.SelectedSource.Type) != 0;
            SlotHighlightUtility.SetHighlight(highlightState, highlight, isHighlighted);
        }
    }
}
