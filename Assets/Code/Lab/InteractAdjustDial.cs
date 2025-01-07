using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InteractAdjustDial : BatchedComponent
    {
        public float BaseVal; // the value the current drag adjusts around
        [NonSerialized] public float CurrVal; // the current raw value on the dial
    }

    public static class DialUtility
    {
        public static void TryAdjustDial(InteractAdjustDial dial, float delta)
        {
            // TODO: bounds check
            dial.CurrVal = dial.BaseVal + delta;
        }
    }
}
