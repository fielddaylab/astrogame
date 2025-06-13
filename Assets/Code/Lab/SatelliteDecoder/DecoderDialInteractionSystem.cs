using Astro;
using BeauRoutine;
using BeauUtil;
using FieldDay;
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
                if (!component.Secondary.InteractReceived) { continue; }

                DecoderUtility.AdjustDecoderDial(component.Primary.Target, component.Primary.Vector);

                // TODO: Quick scroll when button held

                decoderState.InputUpdatedThisFrame = true;
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
            // TODO: make a routine
            dial.Spinner.Rotate(Vector3.forward, dial.FacetAngle * -vector, Space.Self);
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
        public static void UpdateDecoderDialVals(SatelliteDecoderDial dial)
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
        }
    }
}