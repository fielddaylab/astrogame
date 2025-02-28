using BeauRoutine;
using FieldDay;
using FieldDay.Components;
using System;
using System.Collections;
using UnityEngine;

namespace Astro {
    public class PhotometerDial : BatchedComponent, IRegistrationCallbacks {
        public DataDisplay MagnitudeDisplay;
        public Transform NeedlePivot;

        [NonSerialized] public Routine NeedleRoutine;

        public void OnDeregister() {
        }

        public void OnRegister() {
            MagnitudeDisplay.OnDisplayRequested.Register((packet, flags) => PhotometerUtility.UpdateDial(this, packet));
            MagnitudeDisplay.OnDisplayCleared.Register(() => PhotometerUtility.UpdateDial(this));
            PhotometerUtility.UpdateDial(this);
        }
    }

    public static partial class PhotometerUtility {
        public static readonly float MIN_MAG = 5;
        public static readonly float MAX_MAG = -2;
        public static void UpdateDial(PhotometerDial dial, DataPacket packet) {
            if ((packet.Type & DataTypeMask.ApparentMagnitude) != 0 || (packet.Type & DataTypeMask.AbsoluteMagnitude) != 0) {
                dial.NeedleRoutine.Replace(MoveNeedle(dial, MagToDegrees((float)packet.Value.Magnitude)));
            }
        }

        public static void UpdateDial(PhotometerDial dial) {
            UpdateDial(dial, DataPacket.ApparentMagnitude(MIN_MAG));
        }

        public static float MagToDegrees(float magnitude) {
            float dialPercent = Mathf.InverseLerp(MIN_MAG, MAX_MAG, magnitude);
            return Mathf.Lerp(90, -90, dialPercent);

        }

        public static IEnumerator MoveNeedle(PhotometerDial dial, float eulerDegrees) {
            yield return dial.NeedlePivot.RotateTo(eulerDegrees, 0.3f, Axis.Z, Space.Self).Ease(Curve.BackOut);
            yield return null;

        }
    }
}