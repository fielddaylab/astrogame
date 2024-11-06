using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {

    [SysUpdate(GameLoopPhase.Update, 1)]
    public class DocumentInteractionSystem : SharedStateSystemBehaviour<DocumentBoardState> {
        private int DOCUMENT_MASK = -1;
        public override void ProcessWork(float deltaTime) {
            if (!m_State.EnableDocumentInteraction) return;

            if (m_State.SelectedDocument != null && !m_State.DocumentRoutine.Exists()) {
                // waits to complete current routine before moving - "jerky" movement
                DocumentUtility.MoveSelectedToMouse(m_State);
            }

            if (Game.Input.IsMousePressed(FieldDay.HID.MouseButton.Left)) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, DOCUMENT_MASK)) {
                    var doc = hit.collider.GetComponent<DocumentInteractable>();
                    if (doc) {
                        DocumentUtility.SelectDocument(doc, m_State);
                    }
                }
            }
        }
        public override void Initialize() {
            DOCUMENT_MASK = LayerMask.GetMask("DocumentInteract");
        }

    }

    public static partial class DocumentUtility {
        public static void MoveSelectedToMouse(DocumentBoardState state) {
            if (Vector3.Distance(Input.mousePosition, state.LastMousePos) < state.FollowRadius) {
                return;
            }
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            state.LastMousePos = Input.mousePosition;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f)) {
                state.DocumentRoutine.Replace(MoveToPoint(state.SelectedDocument.transform, hit.point, state.FollowSpeed));
            }
        }


        private static IEnumerator MoveToPoint(Transform doc, Vector3 point, float speed) {
            yield return doc.MoveToWithSpeed(new Vector3(point.x, point.y, doc.position.z), speed, Axis.XY).Ease(Curve.Smooth);
        }
    }

}