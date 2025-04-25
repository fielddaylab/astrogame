
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Debugging;
using FieldDay.SharedState;
using System;
using UnityEngine;

namespace Astro {
    public class PlayerPointsState : SharedStateComponent {
        [NonSerialized] public int SciencePoints = 0;
    }
}