using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.HID;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 10)] // After MouseInteractionSystem
    public class InteractAdjustDialSystem : ComponentSystemBehaviour<LabInteractable, InteractAdjustDial>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, InteractAdjustDial secondary, float deltaTime)
        {
            base.ProcessWorkForComponent(primary, secondary, deltaTime);

            if (primary.InteractEnded)
            {
                secondary.BaseVal = secondary.CurrConstrainedVal;
                secondary.PassThroughOffset = 0;
            }

            if (!primary.IsDragging) { return; }

            var interactState = Find.State<LabInteractableState>();
            var delta = interactState.CurrMousePos - interactState.StartMousePos;

            DialUtility.TryAdjustDial(secondary, delta.x);
        }
    }
}
