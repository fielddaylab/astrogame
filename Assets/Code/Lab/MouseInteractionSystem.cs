using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.HID;
using Astro.Reference;
using Leaf.Runtime;
using FieldDay.Scripting;
using BeauUtil;

namespace Astro {
    /// <summary>
    /// Responsible for triggering Lab Interactables
    /// </summary>
    /// 
    [SysUpdate(GameLoopPhase.Update, -1, AstroGame.InteractUpdateMask)]
    public class MouseInteractionSystem : SharedStateSystemBehaviour<LabInteractableState, DocumentBoardState, InputState> {

        public override void ProcessWork(float deltaTime) {
            // on click, try cast ray for lab interactable

            bool isCurrentlyDragging = m_StateA.CurrInteractable && m_StateA.CurrInteractable.IsDragging;
            isCurrentlyDragging |= m_StateB.SelectedDocument;

            if (m_StateC.InputEnabled && !isCurrentlyDragging && Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left) && !Game.Input.AreRaycastsPaused()) {
                var ray = Game.Rendering.PrimaryCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 25f, m_StateC.AppliedLayerMask)) {
                    if (hit.collider.TryGetComponent(out RefGuideControl refControl) && refControl.isActiveAndEnabled) {
                        ReferenceUtility.HandleControl(refControl);
                    }

                    if (hit.collider.TryGetComponent(out DocumentPart docPart) && docPart.isActiveAndEnabled) {
                        DocumentUtility.ProcessDocPartInteraction(docPart, m_StateB);
                        if (m_StateB.InteractedThisFrame) {
                            return;
                        }
                    }

                    if (hit.collider.TryGetComponent(out LabInteractable interactable)) {
                        UseLabInteractable(interactable);
                    }

                    if (hit.collider.TryGetComponent(out ViewLink link) && link.isActiveAndEnabled) {
                        ViewNavUtility.MoveByLink(Find.State<ViewState>(), link);
                    }

                    Game.Input.ConsumeAllInputForFrame();
                }
            }

            // drag
            if (Game.Input.IsMouseDown(FieldDay.HID.MouseButton.Left)) {
                if (m_StateA.CurrInteractable && m_StateA.CurrInteractable.IsDraggable) {
                    m_StateA.CurrMousePos = Input.mousePosition;
                    m_StateA.CurrInteractable.IsDragging = true;
                    CursorHint.TryLock(m_StateA.CurrInteractable.Cursor);
                }
            }

            // mouse up
            if (isCurrentlyDragging && Game.Input.IsMouseUp(FieldDay.HID.MouseButton.Left)) {
                LabInteractableUtility.ReleaseCurrentInteractable();
            }
        }

        private void UseLabInteractable(LabInteractable interactable) {
            if (!interactable.isActiveAndEnabled) {
                return;
            }

            using (var table = TempVarTable.Alloc()) {
                if (interactable.gameObject.TryGetComponent(out ScriptActor actor)) {
                    table.Set("actorId", actor.Id);
                } else if (interactable.gameObject.TryGetComponent(out InteractSelectSlot slot)) {
                    table.Set("actorId", slot.DataSlot.SlotId);
                } else {
                    table.Set("actorId", new StringHash32("Unknown"));
                }
                
                // prevent OnLabInteraction from triggering when OnPuzzleCellSelected should trigger
                if (!interactable.gameObject.TryGetComponent(out InteractSelectPuzzleCell cell)) ScriptUtility.Trigger(ScriptEvents.OnLabInteraction, table);
            }

            interactable.InteractReceived = true;
            m_StateA.CurrInteractable = interactable;
            m_StateA.StartMousePos = Input.mousePosition;
            m_StateA.CurrMousePos = Input.mousePosition;
        } 
    }
}
