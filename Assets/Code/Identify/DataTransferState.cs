using System;
using System.Text;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class DataTransferState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public DataSlot SelectedSource;
        [NonSerialized] public DataSlot SelectedTarget;

        public bool SourceUpdated = false;

        private Action m_ClearSelectedSource;


        public void OnRegister() {
            m_ClearSelectedSource = () => {
                var transferState = Find.State<DataTransferState>();
                var puzzleState = Find.State<PuzzleState>();

                if (!puzzleState.IsIsolated) DataUtility.AssignSelectedSource(transferState, null);
            };
            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, m_ClearSelectedSource);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.MonitorEmptySpaceClicked, m_ClearSelectedSource);
        }

    }

    static public partial class DataUtility {
        static public bool TryTransferData(DataSlot source, DataSlot target) {
            if (!source.HasData || source == target) {
                return false;
            }

            if (TrySetData(target, source.CurrentData) || TrySetData(target, source.SiblingSlot.CurrentData)) {
                source.OnDataTransferred.Invoke(target);
                return true;
            }

            return false;
        }

        static public void AssignSelectedSource(DataTransferState state, DataSlot source, bool preserveTarget = false) {
            state.SelectedSource = source;
            if (!preserveTarget) { state.SelectedTarget = null; }
            state.SourceUpdated = true;
        }

        static public void AssignSelectedTarget(DataTransferState state, DataSlot target) {
            state.SelectedTarget = target;
            state.SourceUpdated = true;
        }

        static public void ClearSelections(DataTransferState state) {
            state.SelectedTarget = null;
            state.SelectedSource = null;
            state.SourceUpdated = true;
        }
    }
}