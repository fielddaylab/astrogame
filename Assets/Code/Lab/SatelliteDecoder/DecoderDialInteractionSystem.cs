using Astro;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 100, AstroGame.InteractUpdateMask)] // After MouseInteractionSystem
    public class DecoderDialInteractionSystem : ComponentSystemBehaviour<SatelliteDecoderDialButton, LabInteractable>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            var decoderState = Find.State<SatelliteDecoderState>();

            foreach (var component in m_Components)
            {
                if (component.Secondary.InteractReceived) {
                    DecoderUtility.AdjustDecoderDial(component.Primary.Target, component.Primary.Vector);
                    decoderState.InputUpdatedThisFrame = true;
                }
                else if (component.Secondary.IsDragging) {
                    // Quick scroll when button held
                    if (component.Primary.Target.HoldTriggerTimer >= component.Primary.Target.HoldTriggerTime) {
                        if (component.Primary.Target.HoldCooldownTimer >= component.Primary.Target.HoldCooldownTime)
                        {
                            DecoderUtility.AdjustDecoderDial(component.Primary.Target, component.Primary.Vector);
                            decoderState.InputUpdatedThisFrame = true;
                            component.Primary.Target.HoldCooldownTimer -= component.Primary.Target.HoldCooldownTime;
                        }
                        component.Primary.Target.HoldCooldownTimer += deltaTime;
                    }
                    component.Primary.Target.HoldTriggerTimer += deltaTime;
                }

                if (component.Secondary.InteractEnded) {
                    component.Primary.Target.HoldTriggerTimer = 0;
                }
            }
        }
    }

    public static partial class DecoderUtility
    {
        public static void AdjustDecoderDial(SatelliteDecoderDial dial, int vector)
        {
            // update current index
            dial.CurrValIndex = ClampedValIndex(dial.CurrValIndex + vector, dial.Values.Length);
            dial.CurrDisplayIndex = ClampedDisplayIndex(dial.CurrDisplayIndex + vector, dial.TextDisplays.Length);
            UpdateDecoderDialVals(dial);

            // update target rotation
            Quaternion newRotation = Quaternion.AngleAxis(dial.FacetAngle * -vector, Vector3.forward);
            dial.CurrTargetRotation = dial.CurrTargetRotation * newRotation;
            dial.RotateRoutine.Replace(SpinnerRotateRoutine(dial));
            dial.CountTimeRoutine.Replace(SpinnerCountTimeRoutine(dial));
        }

        private static IEnumerator SpinnerRotateRoutine(SatelliteDecoderDial dial)
        {
            if (!Sfx.IsActive(dial.RotateAudioHandle) && dial.RotationTime < 0.225f) {
                dial.RotateAudioHandle = Sfx.Play("Oneshot.Dial.Spin");
            }
            else if (dial.RotationTime >= 0.2f) {
                if (!dial.InLongSpin) {
                    Sfx.Stop(dial.RotateAudioHandle);
                    dial.RotateAudioHandle = Sfx.Play("Oneshot.Dial.Longspin");
                    dial.InLongSpin = true;
                }
            }

            yield return dial.Spinner.RotateQuaternionTo(dial.CurrTargetRotation, dial.RotateDuration, Space.Self);

            if (Sfx.IsActive(dial.RotateAudioHandle)) {
                Sfx.Stop(dial.RotateAudioHandle);
            }

            dial.RotationTime = 0;
            dial.InLongSpin = false;

            int prevDisplayIndex = ClampedDisplayIndex(dial.CurrDisplayIndex - 1, dial.TextDisplays.Length);
            int nextDisplayIndex = ClampedDisplayIndex(dial.CurrDisplayIndex + 1, dial.TextDisplays.Length);
            for (int i = 0; i < dial.TextDisplays.Length; i++) {
                if (i == prevDisplayIndex || i == dial.CurrDisplayIndex || i == nextDisplayIndex) {
                    dial.TextDisplays[i].gameObject.SetActive(true);
                }
                else {
                    dial.TextDisplays[i].gameObject.SetActive(false);
                }
            }
        }

        private static IEnumerator SpinnerCountTimeRoutine(SatelliteDecoderDial dial)
        {
            while (dial.RotateRoutine.Exists()) {
                dial.RotationTime += Time.deltaTime;
                yield return null;
            }

            dial.RotationTime = 0;
            dial.InLongSpin = false;
        }

        private static int ClampedValIndex(int unclampedVal, int numVals)
        {
            int clampedVal = unclampedVal;
            if (unclampedVal >= numVals) { clampedVal = unclampedVal % numVals; }
            else if (unclampedVal < 0) { clampedVal = numVals + unclampedVal; }

            return clampedVal;
        }

        private static int ClampedDisplayIndex(int unclampedVal, int numTextDisplays)
        {
            int clampedVal = unclampedVal;
            if (unclampedVal >= numTextDisplays) { clampedVal = unclampedVal % numTextDisplays; }
            else if (unclampedVal < 0) { clampedVal = numTextDisplays + unclampedVal; }

            return clampedVal;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="displayIndex">Index of the text display</param>
        /// <param name="valIndex">Index of the value to be shown in the text display</param>
        public static void UpdateDecoderDialVals(SatelliteDecoderDial dial, bool selectiveHiding = false)
        {
            // set current index
            dial.TextDisplays[dial.CurrDisplayIndex].SetText(dial.Values[dial.CurrValIndex].ToString());

            // set previous index
            int prevValIndex = ClampedValIndex(dial.CurrValIndex - 1, dial.Values.Length);
            int prevDisplayIndex = ClampedDisplayIndex(dial.CurrDisplayIndex - 1, dial.TextDisplays.Length);
            dial.TextDisplays[prevDisplayIndex].SetText(dial.Values[prevValIndex].ToString());

            // set next index
            int nextValIndex = ClampedValIndex(dial.CurrValIndex + 1, dial.Values.Length);
            int nextDisplayIndex = ClampedDisplayIndex(dial.CurrDisplayIndex + 1, dial.TextDisplays.Length);
            dial.TextDisplays[nextDisplayIndex].SetText(dial.Values[nextValIndex].ToString());

            if (selectiveHiding) {
                for (int i = 0; i < dial.TextDisplays.Length; i++) {
                    if (i == prevDisplayIndex || i == dial.CurrDisplayIndex || i == nextDisplayIndex) {
                        dial.TextDisplays[i].gameObject.SetActive(true);
                    }
                    else {
                        dial.TextDisplays[i].gameObject.SetActive(false);
                    }
                }
            }
            else {
                for (int i = 0; i < dial.TextDisplays.Length; i++) {
                    dial.TextDisplays[i].gameObject.SetActive(true);
                }
            }
        }
    }
}