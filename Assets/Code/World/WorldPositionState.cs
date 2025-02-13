using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay.SharedState;
using System;

namespace Astro
{
    public class WorldPositionState : SharedStateComponent 
    {
        [NonSerialized] public bool Initialized = false;
        public EqCoords StartingLookCoords;
    }
}