using FieldDay;
using FieldDay.Audio;
using FieldDay.Scripting;
using FieldDay.Systems;

using UnityEngine;
using BeauRoutine;
using BeauUtil.Debugger;
using System.Collections;

namespace Astro {
    public static class DecoderUtility {
        public static void AdjustDecoderDial(SatelliteDecoderDial dial, int vector) {
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

        private static IEnumerator SpinnerRotateRoutine(SatelliteDecoderDial dial) {
            if (!Sfx.IsActive(dial.RotateAudioHandle) && dial.RotationTime < 0.225f) {
                dial.RotateAudioHandle = Sfx.Play("Oneshot.Dial.Spin");
            } else if (dial.RotationTime >= 0.2f) {
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
                bool hideSelectedDisplay = i == prevDisplayIndex || i == dial.CurrDisplayIndex || i == nextDisplayIndex;

                dial.TextDisplays[i].gameObject.SetActive(hideSelectedDisplay);
            }
        }

        private static IEnumerator SpinnerCountTimeRoutine(SatelliteDecoderDial dial) {
            while (dial.RotateRoutine.Exists()) {
                dial.RotationTime += Time.deltaTime;
                yield return null;
            }

            dial.RotationTime = 0;
            dial.InLongSpin = false;
        }

        private static int ClampedValIndex(int unclampedVal, int numVals) {
            int clampedVal = unclampedVal;
            if (unclampedVal >= numVals) { clampedVal = unclampedVal % numVals; }
            else if (unclampedVal < 0) { clampedVal = numVals + unclampedVal; }

            return clampedVal;
        }

        private static int ClampedDisplayIndex(int unclampedVal, int numTextDisplays) {
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
        public static void UpdateDecoderDialVals(SatelliteDecoderDial dial, bool selectiveHiding = false) {
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
                    bool hideSelectedDisplay = i == prevDisplayIndex || i == dial.CurrDisplayIndex || i == nextDisplayIndex;

                    dial.TextDisplays[i].gameObject.SetActive(hideSelectedDisplay);
                }
            } else {
                for (int i = 0; i < dial.TextDisplays.Length; i++) {
                    dial.TextDisplays[i].gameObject.SetActive(true);
                }
            }
        }

        public static bool AssessSequence(SatelliteDecoderState state) {
            return AssessSequence(state.Dials, state.Solution);
        }

        public static bool AssessSequence(SatelliteDecoderDial[] dials, string solution) {
            Assert.True(solution.Length == dials.Length);

            char dialChar, solutionChar;
            // check each decoder value in turn to see if it matches the solution
            for (int i = 0; i < solution.Length; i++) {
                dialChar = char.ToUpper(dials[i].Values[dials[i].CurrValIndex]);
                solutionChar = char.ToUpper(solution[i]);

                if (!dialChar.Equals(solutionChar)) {
                    return false;
                }
            }

            return true;
        } 
    }
}