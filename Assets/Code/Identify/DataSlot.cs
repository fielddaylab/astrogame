using System;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.Scripting;
using UnityEngine;

namespace Astro {
    public sealed class DataSlot : BatchedComponent, IRegistrationCallbacks {
        public DataSlot SiblingSlot; // Reference to a paired data slot
        public DataTypeMask Type;
        [Required] public DataDisplay[] Displays;
        public bool IsSource;
        public bool IsActive = true;
        public bool IsHidingData;

        public int PuzzleRow = -1;
        public int PuzzleCol = -1;

        [NonSerialized] public bool HasData;
        [NonSerialized] public bool Modifiable = true;
        [NonSerialized] public DataPacket CurrentData;

        public string OverrideSlotId = String.Empty; // Unique id for data slot
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

        public void OnRegister()
        {
            if (!OverrideSlotId.Equals(String.Empty)) {
                SlotId = OverrideSlotId;
            }
        }

        public void OnDeregister()
        {
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
                if (!slot.IsHidingData) {
                    foreach (var display in slot.Displays) {
                        PopulateDisplay(display, packet);
                    }
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

        static public void HideData(DataSlot slot) {
            if (!slot.IsHidingData) {
                slot.IsHidingData = true;
                if (slot.HasData) {
                    foreach (var display in slot.Displays) {
                        ClearDisplay(display);
                    }
                }
            }
        }

        static public void RevealData(DataSlot slot) {
            if (slot.IsHidingData) {
                slot.IsHidingData = false;
                if (slot.HasData) {
                    foreach (var display in slot.Displays) {
                        PopulateDisplay(display, slot.CurrentData);
                    }
                }
            }
        }
    }
}