using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.HID;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 10, AstroGame.InstrumentUpdateMask)] // After MouseInteractionSystem
    public class InteractAdjustDialSystem : ComponentSystemBehaviour<InteractAdjustDial, LabInteractable>
    {
        public override void ProcessWorkForComponent(InteractAdjustDial primary, LabInteractable secondary, float deltaTime)
        {
            if (secondary.InteractEnded)
            {
                primary.BaseVal = primary.CurrConstrainedVal;
                primary.PassThroughOffset = 0;
            }

            if (!secondary.IsDragging) { return; }

            var interactState = Find.State<LabInteractableState>();
            var delta = interactState.CurrMousePos - interactState.StartMousePos;

            DialUtility.TryAdjustDial(primary, delta.x);
        }
    }
}
