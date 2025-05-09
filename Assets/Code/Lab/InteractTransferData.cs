using FieldDay.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InteractTransferData : BatchedComponent
    {
        public DataSlot DataSlot;
    }

    static public partial class DataUtility {
        static public void Rewire(InteractTransferData transfer, DataSlot slot) {
            if (transfer.DataSlot == slot) {
                return;
            }

            if (transfer.DataSlot != null) {
                if (transfer.DataSlot.TryGetComponent(out RelevantSlotHighlight highlight)) {
                    highlight.Ignored = true;
                }
            }

            transfer.DataSlot = slot;
            if (transfer.TryGetComponent(out InteractSelectSlot select)) {
                select.DataSlot = slot;
            }
            if (transfer.TryGetComponent(out RelevantSlotHighlight newHighlight)) {
                newHighlight.Ignored = false;
            }
        }
    }
}
