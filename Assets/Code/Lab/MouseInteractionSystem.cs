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
    public class MouseInteractionSystem : SharedStateSystemBehaviour<LabInteractableState> {
        private int LAB_INTERACT_MASK = -1;
        private int DOCUMENT_MASK = -1;
        private int REFERENCE_MASK = -1;


        public override void ProcessWork(float deltaTime) {
            // on click, try cast ray for lab interactable
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left)) {
                if (Physics.Raycast(ray, out RaycastHit refHit, 10f, REFERENCE_MASK)) {
                    var region = refHit.collider.GetComponent<RefGuideRegion>();
                    if (region) {
                        ReferenceUtility.SelectRegion(region);
                    }
                }

                if (Physics.Raycast(ray, out RaycastHit docHit, 10f, DOCUMENT_MASK)) {
                    if (docHit.collider.TryGetComponent(out DocumentPart docPart)) {
                        DocumentUtility.ProcessDocPartInteraction(docPart);
                        if (Find.State<DocumentBoardState>().InteractedThisFrame) {
                            return;
                        }
                    } 
                }

                if (Physics.Raycast(ray, out RaycastHit labHit, 10f, LAB_INTERACT_MASK)) {
                    var interactable = labHit.collider.GetComponent<LabInteractable>();
                    if (interactable) {
                        interactable.InteractReceived = true;
                        if (!interactable.MaintainExistingView) {
                            ViewNavUtility.MoveToNode(Find.State<ViewState>(), interactable.ConnectedViewNode);
                        }
                        // consume input
                        m_State.CurrInteractable = interactable;
                        m_State.StartMousePos = Input.mousePosition;
                        m_State.CurrMousePos = Input.mousePosition;
                    }
                }

                Game.Input.ConsumeAllInputForFrame();
            }

            // drag
            if (Game.Input.IsMouseDown(FieldDay.HID.MouseButton.Left)) {
                if (m_State.CurrInteractable && m_State.CurrInteractable.IsDraggable)
                {
                    m_State.CurrMousePos = Input.mousePosition;
                    m_State.CurrInteractable.IsDragging = true;
                }
            }

            // mouse up
            if (Game.Input.IsMouseUp(FieldDay.HID.MouseButton.Left))
            {
                if (m_State.CurrInteractable)
                {
                    m_State.CurrInteractable.IsDragging = false;
                    m_State.CurrInteractable.InteractEnded = true;
                    m_State.CurrInteractable = null;
                    m_State.StartMousePos = m_State.CurrMousePos = Vector2.zero;
                }
            }
        }

        public override void Initialize() {
            LAB_INTERACT_MASK = LayerMask.GetMask("LabInteract");
            DOCUMENT_MASK = LayerMask.GetMask("DocumentInteract");
            REFERENCE_MASK = LayerMask.GetMask("ReferenceInteract");
        }
    }
}
