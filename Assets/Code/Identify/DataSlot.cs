using System;
using BeauUtil;
using FieldDay.Components;

namespace Astro {
    public sealed class DataSlot : BatchedComponent {
        public DataTypeMask Type;
        [Required] public DataDisplay[] Displays;

        [NonSerialized] public bool HasData;
        [NonSerialized] public DataPacket CurrentData;

        public readonly CastableEvent<DataPacket> OnDataModified = new CastableEvent<DataPacket>();
        public readonly CastableEvent<DataSlot> OnDataTransferred = new CastableEvent<DataSlot>();
    }

    static public partial class DataUtility {
        static public bool TrySetData(DataSlot slot, DataPacket packet) {
            if ((packet.Type & slot.Type) == 0) {
                return false;
            }

            if (!slot.HasData || !slot.CurrentData.Equals(packet)) {
                slot.HasData = true;
                slot.CurrentData = packet;
                foreach (var display in slot.Displays) {
                    PopulateDisplay(display, packet);
                }
                slot.OnDataModified.Invoke(packet);
            }
            return true;
        }

        static public void ClearData(DataSlot slot) {
            if (slot.HasData) {
                slot.HasData = false;
                slot.CurrentData = default;
                foreach(var display in slot.Displays) {
                    ClearDisplay(display);
                }
                slot.OnDataModified.Invoke(default);
            }
        }
    }
}