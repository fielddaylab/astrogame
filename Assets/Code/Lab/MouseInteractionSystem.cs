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
    public class MouseInteractionSystem : SystemBehaviour {
        private int LAB_INTERACT_MASK = -1;
        private int DOCUMENT_MASK = -1;
        private int REFERENCE_MASK = -1;


        public override void ProcessWork(float deltaTime) {
            // on click, try cast ray for lab interactable
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left)) {
                if (Physics.Raycast(ray, out RaycastHit refHit, Mathf.Infinity, REFERENCE_MASK)) {
                    var region = refHit.collider.GetComponent<RefGuideRegion>();
                    if (region) {
                        ReferenceUtility.SelectRegion(region);
                        Game.Input.ConsumeAllInputForFrame();
                    }
                    return;
                } else if (Physics.Raycast(ray, out RaycastHit labHit, Mathf.Infinity, LAB_INTERACT_MASK)) {
                    var interactable = labHit.collider.GetComponent<LabInteractable>();
                    if (interactable) {
                        interactable.InteractReceived = true;
                        ViewNavUtility.MoveToNode(Find.State<ViewState>(), interactable.ConnectedViewNode);
                        // consume input
                        Game.Input.ConsumeAllInputForFrame();
                    }
                    return;
                } else if (Physics.Raycast(ray, out RaycastHit docHit, Mathf.Infinity, DOCUMENT_MASK)) {
                    var doc = docHit.collider.GetComponent<DocumentInteractable>();
                    if (doc) {
                        DocumentUtility.SelectDocument(doc, Find.State<DocumentBoardState>());
                        Game.Input.ConsumeAllInputForFrame();
                    }
                    return;
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
