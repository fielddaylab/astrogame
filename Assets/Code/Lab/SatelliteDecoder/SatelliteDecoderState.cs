using System;
using FieldDay.SharedState;

namespace Astro {
    public class SatelliteDecoderState : SharedStateComponent {
        public SatelliteDecoderDial[] Dials;
        public string Solution;

        [NonSerialized] public bool InputUpdatedThisFrame;
    }
}
