using System;
using FieldDay.Components;
using FieldDay.SharedState;

namespace Astro {
    public sealed class DataTransferState : SharedStateComponent {
        [NonSerialized] public DataSlot SelectedSource;
        [NonSerialized] public DataSlot SelectedTarget;
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
    }
}