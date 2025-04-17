using System;
using BeauUtil;
using FieldDay.Components;

namespace Astro {
    public sealed class DataSlot : BatchedComponent {
        public DataSlot SiblingSlot; // Reference to a paired data slot
        public DataTypeMask Type;
        [Required] public DataDisplay[] Displays;
        public bool IsSource;
        public bool IsActive = true;

        [NonSerialized] public bool HasData;
        [NonSerialized] public bool Modifiable = true;
        [NonSerialized] public DataPacket CurrentData;

        [NonSerialized] public StringHash32 SlotId = StringHash32.Null; // Unique id for data slot

        public readonly CastableEvent<DataPacket> OnDataModified = new CastableEvent<DataPacket>();
        public readonly CastableEvent<DataSlot> OnDataTransferred = new CastableEvent<DataSlot>();

        #region Overrides

        public bool Equals(DataSlot slot)
        {
            if (slot == null) { return false; }
            return GetInstanceID() == slot.GetInstanceID();
        }

        public override bool Equals(object obj)
        {
            if (obj is DataSlot)
            {
                return Equals((DataSlot)obj);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion // Overrides
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

        static public bool TryClearData(DataSlot slot) {
            if (slot.Modifiable) {
                ClearData(slot);
                return true;
            }
            return false;
        }
    }
}