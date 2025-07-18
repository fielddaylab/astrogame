using BeauRoutine;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 100, AstroGame.InteractUpdateMask)] // After MouseInteractionSystem
    public class DecoderDialInteractionSystem : ComponentSystemBehaviour<SatelliteDecoderDialButton, LabInteractable> {
        public override void ProcessWork(float deltaTime) {
            base.ProcessWork(deltaTime);

            var decoderState = Find.State<SatelliteDecoderState>();

            foreach (var component in m_Components) {
                if (component.Secondary.InteractReceived) {
                    DecoderUtility.AdjustDecoderDial(component.Primary.Target, component.Primary.Vector);
                    decoderState.InputUpdatedThisFrame = true;
                } else if (component.Secondary.IsDragging) {
                    // Quick scroll when button held
                    if (component.Primary.Target.HoldTriggerTimer >= component.Primary.Target.HoldTriggerTime) {
                        if (component.Primary.Target.HoldCooldownTimer >= component.Primary.Target.HoldCooldownTime) {
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
}