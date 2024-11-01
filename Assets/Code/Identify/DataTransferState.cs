using System;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class DataTransferState : SharedStateComponent {
        [NonSerialized] public DataSlot SelectedSource;
        [NonSerialized] public DataSlot SelectedTarget;

        public bool SourceUpdated = false;
    }

    static public partial class DataUtility {
        static public bool TryTransferData(DataSlot source, DataSlot target) {
            if (!source.HasData || source == target) {
                return false;
            }

            if (TrySetData(target, source.CurrentData)) {
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
    }
}