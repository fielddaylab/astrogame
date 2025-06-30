using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class InteractAdjustDial : BatchedComponent, IRegistrationCallbacks
    {
        public delegate bool CanAdjustPredicate(InteractAdjustDial dial, Vector2 delta);

        [Header("Parameters")]
        public float BaseVal; // the value the current drag adjusts around
        public float InputSensitivity = 1; // how sensitive the dial is to input
        public float RotateSpeed = 1; // how quickly the dial rotates according to input
        public Axis PivotAxis;

        [NonSerialized] public float CurrRawVal; // the current raw value on the dial
        [NonSerialized] public float CurrConstrainedVal; // the current constrained value on the dial
        [NonSerialized] public float ConstrainedValDelta = 0;

        [NonSerialized] public bool ValChanged = false;

        [Header("Objects")]
        public Transform DialRoot;

        public CanAdjustPredicate CanAdjust;

        public void OnDeregister()
        {

        }

        public void OnRegister()
        {
            CurrRawVal = BaseVal;
            CurrConstrainedVal = BaseVal;
        }
    }

    public static class DialUtility
    {
        public static void TryAdjustDial(InteractAdjustDial dial, float delta)
        {
            var preConstrainedVal = dial.CurrConstrainedVal;
            var preRawVal = dial.CurrRawVal;

            var postRawVal = dial.BaseVal + delta * dial.InputSensitivity;

            dial.CurrConstrainedVal = Math.Clamp(postRawVal, 0, 1);
            var rawValDelta = postRawVal - preRawVal;

            /* TODO: passthrough logic
            if (rawValDelta > 0 && dial.CurrConstrainedVal == 0)
            {
                rawValDelta = 0;
                dial.PassThroughOffset = postRawVal;
            }
            else if (rawValDelta > 1 && dial.CurrConstrainedVal == 1)
            {
                rawValDelta = 1;
                dial.PassThroughOffset = postRawVal;
            }
            */

            // Log.Msg("adjusted dial ({0}/{1}) by {1} to ({2}/{3})", preRawVal, preConstrainedVal, delta * dial.InputSensitivity, postRawVal, dial.CurrConstrainedVal);

            dial.CurrRawVal = postRawVal;
            dial.ConstrainedValDelta = dial.CurrConstrainedVal - preConstrainedVal;

            dial.ValChanged = true;
        }

        public static void TrySetDial(InteractAdjustDial dial, float value) {
            var valDelta = value - dial.CurrConstrainedVal;
            var inputDelta = valDelta / dial.InputSensitivity;
            TryAdjustDial(dial, inputDelta);
            dial.BaseVal = value;
        }
    }
}
