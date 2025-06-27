using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Components;
using TMPro;
using System;

namespace Astro
{
    public class DialAdjustableInstrument : BatchedComponent
    {
        public InteractAdjustDial Source;

        public float LinearMap = 1;
        public float Offset = 0;

        // Any fields or components that are adjusted by dial
        public TMP_Text Readout;
        public string ReadoutSuffix;

        [NonSerialized] public int CurrentValue;
        [NonSerialized] public bool Updated;
    }

    static public partial class InstrumentUtility {
        static public void TrySetValue(DialAdjustableInstrument instrument, int value) {
            float clampedVal = Mathf.Clamp01((value - instrument.Offset) / instrument.LinearMap);
            DialUtility.TrySetDial(instrument.Source, clampedVal);
        }
    }
}