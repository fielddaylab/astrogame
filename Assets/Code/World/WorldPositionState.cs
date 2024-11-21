using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay.SharedState;

namespace Astro
{
    public class WorldPositionState : SharedStateComponent 
    {
        [HideInInspector] public bool Initialized = false;
        public EqCoords StartingLookCoords;
    }
}