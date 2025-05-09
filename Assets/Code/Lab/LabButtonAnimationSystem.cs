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
    public class LabButtonAnimationSystem : ComponentSystemBehaviour<LabButton, LabInteractable>
    {
        public override void ProcessWorkForComponent(LabButton button, LabInteractable secondary, float deltaTime)
        {
            if (!secondary.InteractReceived) { return; }

            if (button.IsToggle) {
                if (!button.AutoToggle) {
                    return;
                }

                if (button.CurrentState == LabButton.State.Up) {
                    LabButtonUtility.SetDown(button, true);
                } else {
                    LabButtonUtility.SetUp(button, true);
                }
            } else {
                LabButtonUtility.SetClicked(button, true);
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

        static public void SetDown(LabButton button, bool playSfx) {
            if (button.CurrentState == LabButton.State.Down) {
                return;
            }

            if (playSfx) {
                Sfx.PlayDetached(button.ToggleSfx, button.Movable);
            }

            button.CurrentState = LabButton.State.Down;

            button.Movable.localPosition = button.OriginalDisplacement;
            button.TransitionRoutine.Replace(button, LabButtonUtility.AnimateToggleDown(button));
        }

        static public void SetUp(LabButton button, bool playSfx) {
            if (button.CurrentState == LabButton.State.Up) {
                return;
            }

            if (playSfx) {
                Sfx.PlayDetached(button.UntoggleSfx, button.Movable);
            }

            button.CurrentState = LabButton.State.Up;

            button.Movable.localPosition = button.OriginalDisplacement + button.LocalDisplacement;
            button.TransitionRoutine.Replace(button, LabButtonUtility.AnimateToggleUp(button));
        }

        static public void SetClicked(LabButton button, bool playSfx) {
            if (playSfx) {
                Sfx.PlayDetached(button.ClickSfx, button.Movable);
            }

            button.CurrentState = LabButton.State.Click;

            button.Movable.localPosition = button.OriginalDisplacement;
            button.TransitionRoutine.Replace(button, LabButtonUtility.AnimateClick(button));
        }

        static public void ForceDown(LabButton button) {
            button.CurrentState = LabButton.State.Down;
            button.TransitionRoutine.Stop();
            button.Movable.localPosition = button.OriginalDisplacement + button.LocalDisplacement;
        }

        static public void ForceUp(LabButton button) {
            button.CurrentState = LabButton.State.Up;
            button.TransitionRoutine.Stop();
            button.Movable.localPosition = button.OriginalDisplacement;
        }
    }
}