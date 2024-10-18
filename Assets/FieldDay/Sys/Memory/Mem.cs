using BeauPools;
using UnityEngine;

namespace FieldDay.Memory {

    static public class Mem {
        static internal MemoryMgr Mgr;

        static public event GCNotifyDelegate OnGarbageCollectionOccurred;

        static internal void InvokeGCOccurred(int mask) {
            OnGarbageCollectionOccurred?.Invoke(mask);
        }
    }

    public delegate void GCNotifyDelegate(int generationMask);
}