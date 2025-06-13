using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SatelliteDecoderState : SharedStateComponent
    {
        public SatelliteDecoderDial[] Dials;
        public string Solution;

        [NonSerialized] public bool InputUpdatedThisFrame;
    }
}
