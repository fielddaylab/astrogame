using BeauUtil;
using FieldDay.SharedState;
using System;
using System.Collections.Generic;

namespace Astro {
    public sealed class PlayerProgressState : ISharedState {
        public int DayIndex = 0;

        public Dictionary<StringHash32, BitSet32> Classifications = MapUtils.Create<StringHash32, BitSet32>(64);
        [NonSerialized] public List<ArchiveLayout> DayLayouts = new List<ArchiveLayout>();
    }
}