using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using BeauRoutine;
using FieldDay.Scripting;
using System.Text;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 500, AstroGame.AnySubmissionUpdateMask)] // After Interactable Select System
    public class InteractSelectSlotSystem : ComponentSystemBehaviour<InteractSelectSlot, LabInteractable> {
        private static PacketTransferData m_WorkingPacketTransferData = new PacketTransferData();
        private static StringBuilder m_WorkingStringBuilder = new StringBuilder();

        public override bool HasWork() {
            return base.HasWork() && (Find.State<ViewState>().ActiveNode?.AllowSlotSelection ?? false);
        }

        public override void ProcessWorkForComponent(InteractSelectSlot primary, LabInteractable secondary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }

            if (primary.DataSlot != null && primary.DataSlot.IsSource && primary.DataSlot.Displays.Length > 0) {
                m_WorkingStringBuilder.Clear();
                // Assemble Analytics data
                // m_WorkingPacketTransferData.StarId
                m_WorkingPacketTransferData.ToolId = DataUtility.GetInstrumentTypeFromDataMask(primary.DataSlot.Type);
                
                if (primary.DataSlot.CurrentData.IsValid) {
                    if (DataUtility.TryFormatForDefaultOutput(primary.DataSlot.CurrentData, primary.DataSlot.Displays[0].Formatting, m_WorkingStringBuilder)) {
                        m_WorkingPacketTransferData.ValueStr = m_WorkingStringBuilder.ToString();
                    }
                }
                else {
                    string nullTxt = primary.DataSlot.Displays[0].NullText;
                    if (string.IsNullOrEmpty(nullTxt)) {
                        nullTxt = DataUtility.EMPTY_OUTPUT;
                    }
                    m_WorkingStringBuilder.Append(nullTxt);
                }
                m_WorkingPacketTransferData.ValueStr = m_WorkingStringBuilder.ToString();

                AstroGame.Events.Dispatch(GameEvents.ClickToolLoad, EvtArgs.Box(m_WorkingPacketTransferData));
            }

            if (!primary.DataSlot.IsActive || primary.DataSlot.IsHidingData) { return; }

            var transferState = Find.State<DataTransferState>();
            var cancelInputState = Find.State<CancelInputState>();
            cancelInputState.SlotClicked = true;

            // If puzzle is locked, only run the following logic on an isolated slot
            var puzzleState = Find.State<PuzzleState>();
            if (puzzleState.IsIsolated && !PuzzleUtility.IsSlotIsolated(puzzleState, primary.DataSlot.SlotId)) { return; }

            // If nothing selected, 
            if (transferState.SelectedSource == null && transferState.SelectedTarget == null) {
                // and component can be a source, 
                if (primary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, primary.DataSlot);
                } else {
                    DataUtility.AssignSelectedTarget(transferState, primary.DataSlot);
                }
            }
            // Some source is selected (but no target)
            else if (transferState.SelectedSource != null)
            {
                // If source type matches this OR  source sibling exists and its type matches this 
                if (((transferState.SelectedSource.Type & primary.DataSlot.Type) != 0 
                        || (transferState.SelectedSource.SiblingSlot != null 
                            && (transferState.SelectedSource.SiblingSlot.Type & primary.DataSlot.Type) != 0))
                    // AND source or sibling is not itself the target
                    && (!transferState.SelectedSource.Equals(primary.DataSlot) || (transferState.SelectedSource.SiblingSlot != null && !transferState.SelectedSource.SiblingSlot.Equals(primary.DataSlot)))
                    // AND target is modifiable
                    && primary.DataSlot.Modifiable
                    && !primary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedTarget(transferState, primary.DataSlot);
                }
                // Else selected is not a valid target. If it is a valid source, set as current source
                else if (primary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, primary.DataSlot);
                }
            }
            // Some target is selected (but no source)
            else if (transferState.SelectedTarget != null) {
                // if the target type matches this OR this sibling exists and its type matches target
                if (((transferState.SelectedTarget.Type & primary.DataSlot.Type) != 0 || (primary.DataSlot.SiblingSlot != null && (transferState.SelectedTarget.Type & primary.DataSlot.SiblingSlot.Type) != 0))
                    // AND SelectedTarget is not itself the clicked source
                    && (!transferState.SelectedTarget.Equals(primary.DataSlot))
                    && primary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedSource(transferState, primary.DataSlot, true);

                } else if (!primary.DataSlot.IsSource) {
                    DataUtility.AssignSelectedTarget(transferState, primary.DataSlot);
                }
            }
        }
    }
}