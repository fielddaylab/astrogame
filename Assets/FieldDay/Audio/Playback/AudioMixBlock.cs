using System;
using UnityEngine;

namespace FieldDay.Audio {
    internal struct AudioMixBlock {
        public float MixFactor;
        public unsafe fixed float Volume[AudioMgr.MaxBuses];
        public unsafe fixed float Pitch[AudioMgr.MaxBuses];
        public unsafe fixed float Pan[AudioMgr.MaxBuses];
        public unsafe fixed float LoPass[AudioMgr.MaxBuses];
        public unsafe fixed float HiPass[AudioMgr.MaxBuses];
    }
}