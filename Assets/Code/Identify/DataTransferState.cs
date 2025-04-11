using System;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class DataTransferState : SharedStateComponent, IRegistrationCallbacks {
        [NonSerialized] public DataSlot SelectedSource;
        [NonSerialized] public DataSlot SelectedTarget;

        public bool SourceUpdated = false;

        public void OnDeregister()
        {
        }

        public void OnRegister()
        {
            Game.Events.Register(GameEvents.MonitorEmptySpaceClicked, () => {
                var transferState = Find.State<DataTransferState>();
                DataUtility.AssignSelectedSource(transferState, null); 
            });
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

        static public void AssignSelectedSource(DataTransferState state, DataSlot source, bool preserveTarget = false)
        {
            state.SelectedSource = source;
            if (!preserveTarget) { state.SelectedTarget = null; }
            state.SourceUpdated = true;
        }

        static public void AssignSelectedTarget(DataTransferState state, DataSlot target)
        {
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