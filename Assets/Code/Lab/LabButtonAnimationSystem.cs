using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;
using FieldDay.Audio;
using BeauRoutine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 550, AstroGame.InteractUpdateMask)] // After Interactable Select System
    public class LabButtonAnimationSystem : ComponentSystemBehaviour<LabInteractable, LabButton>
    {
        public override void ProcessWorkForComponent(LabInteractable primary, LabButton secondary, float deltaTime)
        {
            if (!primary.InteractReceived) { return; }

            if (secondary.IsToggle) {
                if (secondary.CurrentState == LabButton.State.Up) {
                    Sfx.PlayDetached(secondary.ToggleSfx, secondary.Movable);
                    secondary.CurrentState = LabButton.State.Down;

                    secondary.Movable.localPosition = secondary.OriginalDisplacement;
                    secondary.TransitionRoutine.Replace(secondary, LabButtonUtility.AnimateToggleDown(secondary));
                } else {
                    Sfx.PlayDetached(secondary.UntoggleSfx, secondary.Movable);
                    secondary.CurrentState = LabButton.State.Up;

                    secondary.Movable.localPosition = secondary.OriginalDisplacement + secondary.LocalDisplacement;
                    secondary.TransitionRoutine.Replace(secondary, LabButtonUtility.AnimateToggleUp(secondary));
                }
            } else {
                Sfx.PlayDetached(secondary.ClickSfx, secondary.Movable);
                secondary.CurrentState = LabButton.State.Click;

                secondary.Movable.localPosition = secondary.OriginalDisplacement;
                secondary.TransitionRoutine.Replace(secondary, LabButtonUtility.AnimateClick(secondary));
            }
        }
    }

    static public class LabButtonUtility {
        static public IEnumerator AnimateClick(LabButton button) {
            yield return button.Movable.MoveTo(button.OriginalDisplacement + button.LocalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.BackOut);
            yield return button.Movable.MoveTo(button.OriginalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.CubeIn);
            button.CurrentState = LabButton.State.Up;
        }

        static public IEnumerator AnimateToggleDown(LabButton button) {
            return button.Movable.MoveTo(button.OriginalDisplacement + button.LocalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.BackOut);
        }

        static public IEnumerator AnimateToggleUp(LabButton button) {
            return button.Movable.MoveTo(button.OriginalDisplacement, 0.1f, Axis.XYZ, Space.Self).Ease(Curve.BackOut);
        }
    }
}