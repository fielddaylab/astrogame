using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    /// <summary>
    /// Responsible for triggering Lab Interactables
    /// </summary>
    /// 
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class LabInteractTriggerSystem : SystemBehaviour
    {
        private int INTERACTABLE_MASK = -1;

        public override void ProcessWork(float deltaTime)
        {
            // on click, try cast ray for lab interactable
            if (Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, INTERACTABLE_MASK)) {
                    var interactable = hit.collider.GetComponent<LabInteractable>();
                    if (interactable) {
                        interactable.InteractReceived = true;

                        // consume input
                        Game.Input.ConsumeAllInputForFrame();
                    }
                }
            }
        }

        public override void Initialize()
        {
            INTERACTABLE_MASK = LayerMask.GetMask("LabInteract");
        }
    }
}
