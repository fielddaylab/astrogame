using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 2000, AstroGame.SubmissionUpdateMask)] // After InteractSelectSlotSystem
    public class SlotEffectSystem : ComponentSystemBehaviour<DataSlot, RelevantSlotHighlight> {
        public override void ProcessWorkForComponent(DataSlot component, RelevantSlotHighlight highlight, float deltaTime) {
            var transferState = Find.State<DataTransferState>();
            if (!transferState.SourceUpdated) { return; }
            if (transferState.SelectedSource != null && !transferState.SelectedSource.IsSource) { return; }
            if (highlight.Dimmed) { return; }
            // We will not highlight grid/buttons if the player has no star selected
            if (Find.State<FocusState>().CurrentFocus == null) return;

            var highlightState = Find.State<SlotHighlightState>();

            if (component.IsSource) { // Instrument Load Button (source) highlights
                if (component.IsActive) {
                    TryHighlightInstrumentButton(transferState, highlightState, highlight, component);
                }
            } else { // Puzzle Cell (target) highlights
                TryHighlightPuzzleCell(transferState, highlightState, highlight, component);
            }
        }

        private bool TryHighlightPuzzleCell(DataTransferState transferState, SlotHighlightState highlightState, RelevantSlotHighlight highlight, DataSlot slot) {
            bool sourceNotNull = transferState.SelectedSource != null;
            bool targetIsNull = transferState.SelectedTarget == null;
            bool hasSibling = sourceNotNull && transferState.SelectedSource.SiblingSlot != null;
            bool typesMatch = sourceNotNull && ((slot.Type & transferState.SelectedSource.Type) != 0 || (hasSibling && (slot.Type & transferState.SelectedSource.SiblingSlot.Type) != 0));
            bool slotIsModifiable = slot.Modifiable;
            bool isHighlightedAvailable = sourceNotNull && targetIsNull && typesMatch && slotIsModifiable;
            bool isHighlightedSelected = slot.Equals(transferState.SelectedTarget);

            if (isHighlightedSelected) {
                SlotHighlightUtility.SetSelectedHighlight(highlightState, highlight, true);
                return true;
            } else if (isHighlightedAvailable) {
                SlotHighlightUtility.SetAvailableHighlight(highlightState, highlight, true);
                return true;
            } else {
                SlotHighlightUtility.SetSelectedHighlight(highlightState, highlight, false);
                return false;
            }
        }

        private bool TryHighlightInstrumentButton(DataTransferState transferState, SlotHighlightState highlightState, RelevantSlotHighlight highlight, DataSlot slot) {
            bool targetNotNull = transferState.SelectedTarget != null;
            bool sourceIsNull = transferState.SelectedSource == null;
            bool typesMatch = targetNotNull && ((transferState.SelectedTarget.Type & slot.Type) != 0);
            bool siblingExists = slot.SiblingSlot != null;
            bool siblingMatches = targetNotNull && siblingExists && ((transferState.SelectedTarget.Type & slot.SiblingSlot.Type) != 0);
            bool targetModifiable = targetNotNull && transferState.SelectedTarget.Modifiable;

            bool slotIsMatchingInstrument = targetNotNull && sourceIsNull && (typesMatch || siblingMatches) && targetModifiable;
            bool slotIsSelectedInstrument = slot.Equals(transferState.SelectedSource);
            if (slotIsMatchingInstrument) {
                SlotHighlightUtility.SetInstrumentHighlight(highlightState, highlight, true);
                return true;
            } else if (slotIsSelectedInstrument) {
                SlotHighlightUtility.SetInstrumentHighlight(highlightState, highlight, true);
                return true;
            } else {
                SlotHighlightUtility.SetInstrumentHighlight(highlightState, highlight, false);
                return false;
            }
        }
    }
}
