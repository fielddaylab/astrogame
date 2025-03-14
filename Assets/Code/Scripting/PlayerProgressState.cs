using BeauUtil;
using FieldDay.SharedState;
using System.Collections.Generic;

namespace Astro {
    public sealed class PlayerProgressState : ISharedState {
        public int DayIndex = 0;

        public Dictionary<StringHash32, BitSet32> Classifications;
    }
}