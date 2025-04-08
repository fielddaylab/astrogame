using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using FieldDay.HID;
using Astro.Reference;

namespace Astro {
    /// <summary>
    /// Responsible for triggering Lab Interactables
    /// </summary>
    /// 
    [SysUpdate(GameLoopPhase.Update, 0)]
    public class MouseInteractionSystem : SharedStateSystemBehaviour<LabInteractableState, DocumentBoardState, InputState> {

        public override void ProcessWork(float deltaTime) {
            // on click, try cast ray for lab interactable

            bool isCurrentlyDragging = m_StateA.CurrInteractable && m_StateA.CurrInteractable.IsDragging;
            isCurrentlyDragging |= m_StateB.SelectedDocument;

            if (m_StateC.InputEnabled && !isCurrentlyDragging && Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left) && !Game.Input.AreRaycastsPaused()) {
                var ray = Game.Rendering.PrimaryCamera.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, 10f, m_StateC.ClickableLayerMask)) {

                    if (hit.collider.TryGetComponent(out RefGuideControl refControl)) {
                        ReferenceUtility.HandleControl(refControl);
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

                    if (hit.collider.TryGetComponent(out ViewLink link)) {
                        ViewNavUtility.MoveByLink(Find.State<ViewState>(), link);
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
                    CursorHint.TryLock(m_StateA.CurrInteractable.Cursor);
                }
            }

            // mouse up
            if (isCurrentlyDragging && Game.Input.IsMouseUp(FieldDay.HID.MouseButton.Left))
            {
                if (m_StateA.CurrInteractable)
                {
                    CursorHint.Unlock(m_StateA.CurrInteractable.Cursor);
                    m_StateA.CurrInteractable.IsDragging = false;
                    m_StateA.CurrInteractable.InteractEnded = true;
                    m_StateA.CurrInteractable = null;
                    m_StateA.StartMousePos = m_StateA.CurrMousePos = Vector2.zero;
                }
            }
        }

        private void UseLabInteractable(LabInteractable interactable) {
            interactable.InteractReceived = true;
            m_StateA.CurrInteractable = interactable;
            m_StateA.StartMousePos = Input.mousePosition;
            m_StateA.CurrMousePos = Input.mousePosition;
        }
    }
}
