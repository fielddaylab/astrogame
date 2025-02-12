using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {

    [SysUpdate(GameLoopPhase.Update, 1)]
    public class DocumentInteractionSystem : SharedStateSystemBehaviour<DocumentBoardState> {
        public override void ProcessWork(float deltaTime) {
            m_State.InteractedThisFrame = false;
            if (!m_State.EnableDocumentInteraction) return;

            if (m_State.SelectedDocument != null && !m_State.DocumentRoutine.Exists()) {
                DocumentUtility.MoveSelectedToMouse(m_State);
            }

        }

    }

    public static partial class DocumentUtility {
        public static void MoveSelectedToMouse(DocumentBoardState state) {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            state.LastMousePos = Input.mousePosition;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMask.GetMask("DocumentSurface"))) {
                // lerp document from current position to new target position
                LerpToTarget(state.SelectedDocument.transform, hit.transform.InverseTransformPoint(hit.point), state.FollowSpeed);
                //state.DocumentRoutine.Replace(MoveToPoint(state.SelectedDocument.transform, hit.point, state.FollowSpeed));
            } else {
                DeselectDocument(state);
            }
        }

        private static void LerpToTarget(Transform transform, Vector3 target, float percent) {
            transform.SetPosition(Vector3.Lerp(transform.localPosition, target, percent), Axis.XY, Space.Self);
            if (Vector3.Distance(transform.localPosition, target) < 0.1f) {
                transform.SetPosition(target, Axis.XYZ, Space.Self);
            }
        }
    }
}