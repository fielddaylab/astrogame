
using System;
using FieldDay.SharedState;

namespace Astro {
    public class PlayerPointsState : SharedStateComponent {
        [NonSerialized] public int SciencePoints = 0;
    }
}