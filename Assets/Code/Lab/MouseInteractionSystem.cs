using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro {
    /// <summary>
    /// Responsible for triggering Lab Interactables
    /// </summary>
    /// 
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class MouseInteractionSystem : SharedStateSystemBehaviour<LabInteractableState, DocumentBoardState, InputState> {

        public override void ProcessWork(float deltaTime) {
            // on click, try cast ray for lab interactable

            if (Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left)) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 10f, m_StateC.ClickableLayerMask)) {

                    if (hit.collider.TryGetComponent(out RefGuideRegion refRegion)) {
                        ReferenceUtility.SelectRegion(refRegion);
                    }

                    if (hit.collider.TryGetComponent(out DocumentPart docPart)) {
                        DocumentUtility.ProcessDocPartInteraction(docPart, m_StateB);
                        if (m_StateB.InteractedThisFrame) {
                            return;
                        }
                    }
                    if (hit.collider.TryGetComponent(out LabInteractable interactable)) {
                        UseLabInteractable(interactable);
                    }

                    Game.Input.ConsumeAllInputForFrame();
                }

            }

            // drag
            if (Game.Input.IsMouseDown(FieldDay.HID.MouseButton.Left)) {
                if (m_StateA.CurrInteractable && m_StateA.CurrInteractable.IsDraggable)
                {
                    m_StateA.CurrMousePos = Input.mousePosition;
                    m_StateA.CurrInteractable.IsDragging = true;
                }
            }

            // mouse up
            if (Game.Input.IsMouseUp(FieldDay.HID.MouseButton.Left))
            {
                if (m_StateA.CurrInteractable)
                {
                    m_StateA.CurrInteractable.IsDragging = false;
                    m_StateA.CurrInteractable.InteractEnded = true;
                    m_StateA.CurrInteractable = null;
                    m_StateA.StartMousePos = m_StateA.CurrMousePos = Vector2.zero;
                }
            }
        }

        private void UseLabInteractable(LabInteractable interactable) {
            interactable.InteractReceived = true;
            if (!interactable.MaintainExistingView) {
                ViewNavUtility.MoveToNode(Find.State<ViewState>(), interactable.ConnectedViewNode);
            }
            m_StateA.CurrInteractable = interactable;
            m_StateA.StartMousePos = Input.mousePosition;
            m_StateA.CurrMousePos = Input.mousePosition;
        }
    }
}
