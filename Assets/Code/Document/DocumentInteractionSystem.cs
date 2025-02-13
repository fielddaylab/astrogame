using BeauRoutine;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.HID;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {

    [SysUpdate(GameLoopPhase.Update, 1)]
    public class DocumentInteractionSystem : SharedStateSystemBehaviour<DocumentBoardState, InputState> {
        public override void ProcessWork(float deltaTime) {
            m_StateA.InteractedThisFrame = false;
            if (!m_StateA.EnableDocumentInteraction) return;

            if (m_StateA.SelectedDocument != null) {
                if (!m_StateA.DocumentRoutine.Exists()) {
                    DocumentUtility.MoveSelectedToMouse(m_StateA);
                }
                if (m_StateB.InputEnabled && Game.Input.IsMousePressed(MouseButton.Left)) {
                    DocumentUtility.DeselectDocument(m_StateA);
                }
            }

        }

    }

    public static partial class DocumentUtility {
        public static void MoveSelectedToMouse(DocumentBoardState state) {
            var ray = Game.Rendering.PrimaryCamera.ScreenPointToRay(Input.mousePosition);
            state.LastMousePos = Input.mousePosition;
            if (Physics.Raycast(ray, out RaycastHit hit, 10f, LayerMasks.DocumentSurface_Mask)) {
                // lerp document from current position to new target position
                LerpToTarget(state.SelectedDocument.transform, state.SelectedDocument.transform.parent.InverseTransformPoint(hit.point), state.FollowSpeed);
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