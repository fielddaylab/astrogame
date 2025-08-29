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
using FieldDay.Audio;
using FieldDay.Debugging;

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

            bool allowClick = m_StateC.InputEnabled && !isCurrentlyDragging && !Game.Input.AreRaycastsPaused();
            bool isHoldingFreeLook = allowClick && Game.Input.IsKeyDown(KeyCode.Space);

            m_StateC.Raycaster.enabled = !isHoldingFreeLook;

            if (allowClick && Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left)) {
                var ray = Game.Rendering.PrimaryCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                bool hitRaycast = false;
                if (!isHoldingFreeLook) {
                    if (Physics.Raycast(ray, out hit, 25f, m_StateC.AppliedLayerMask)) {
                        if (hit.collider.TryGetComponent(out RefGuideControl refControl) && refControl.isActiveAndEnabled) {
                            ReferenceUtility.HandleControl(refControl);
                            hitRaycast = true;
                        }

                        if (hit.collider.TryGetComponent(out DocumentPart docPart) && docPart.isActiveAndEnabled) {
                            hitRaycast = true;
                            DocumentUtility.ProcessDocPartInteraction(docPart, m_StateB);
                            if (m_StateB.InteractedThisFrame) {
                                return;
                            }
                        }

                        if (hit.collider.TryGetComponent(out LabInteractable interactable)) {
                            hitRaycast = UseLabInteractable(interactable);
                        }

                        if (hit.collider.TryGetComponent(out ViewLink link) && link.isActiveAndEnabled) {
                            ViewNavUtility.MoveByLink(Find.State<ViewState>(), link);
                            hitRaycast = true;
                        }
                    }
                } else {
                    if (!hitRaycast && Physics.Raycast(ray, out hit, 14f, LayerMasks.Tappable_Mask)) {
                        if (hit.collider.TryGetComponent(out TappableCollider tap) && tap.isActiveAndEnabled) {
                            TappableMaterial tapMat = Find.NamedAsset<TappableMaterial>(tap.Material);
                            Sfx.PlayDetached(tapMat.Sound, hit.point, Quaternion.identity);
                            DebugDraw.AddPoint(hit.point, 0.1f, Color.yellow, 1);
                            DebugDraw.AddLogText(string.Format("tapped on {0} at distance {1}", tap.Material.ToDebugString(), hit.distance), Color.yellow, 1);
                            hitRaycast = true;
                        }
                    }
                }

                if (hitRaycast) {
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

        private bool UseLabInteractable(LabInteractable interactable) {
            if (!interactable.isActiveAndEnabled) {
                return false;
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
            return true;
        } 
    }
}
