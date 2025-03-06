using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.Audio;
using BeauRoutine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 550)] // After Interactable Select System
    public class LabButtonAnimationSystem : ComponentSystemBehaviour<LabInteractable, LabButton>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, LabButton secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }

            Sfx.PlayDetached(secondary.ClickSfx, secondary.Movable);
            secondary.CurrentState = LabButton.State.Click;

            secondary.Movable.localPosition = secondary.OriginalDisplacement;
            secondary.TransitionRoutine.Replace(secondary, AnimateClick(secondary));
        }

        static private IEnumerator AnimateClick(LabButton button) {
            yield return button.Movable.MoveTo(button.OriginalDisplacement + button.LocalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.BackOut);
            yield return button.Movable.MoveTo(button.OriginalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.CubeIn);
            button.CurrentState = LabButton.State.Up;
        }
    }
}