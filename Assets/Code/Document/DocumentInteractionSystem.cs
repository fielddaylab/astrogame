using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.HID;
using FieldDay.Systems;
using System.Collections;
using UnityEngine;

namespace Astro {

    [SysUpdate(GameLoopPhase.Update, 1, AstroGame.DocumentUpdateMask)]
    public class DocumentInteractionSystem : SharedStateSystemBehaviour<DocumentBoardState, InputState> {
        public override void ProcessWork(float deltaTime) {
            m_StateA.InteractedThisFrame = false;
            if (!m_StateA.EnableDocumentInteraction) return;

            if (m_StateA.SelectedDocument != null) {
                if (!m_StateA.SpawnDocumentToCamera.Exists()) {
                    DocumentUtility.MoveSelectedToMouse(m_StateA);
                }
                if (m_StateB.InputEnabled && Game.Input.IsMousePressed(MouseButton.Left)) {
                    DocumentUtility.DeselectDocument(m_StateA);
                    Game.Input.ConsumeAllInputForFrame();
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
                Vector3 clampedPoint = state.SelectedDocument.transform.parent.InverseTransformPoint(hit.point);
                clampedPoint = ClampWithinBoard(state, clampedPoint, state.SelectedDocument.Renderer.Size);
                LerpToTarget(state.SelectedDocument.transform, clampedPoint, state.FollowSpeed);
                //state.DocumentRoutine.Replace(MoveToPoint(state.SelectedDocument.transform, hit.point, state.FollowSpeed));
            } else {
                DeselectDocument(state);
            }
        }

        public static Vector2 ClampWithinBoard(DocumentBoardState state, Vector3 localPosition, Rect documentSize) {
            Vector2 offset = (Vector2) localPosition + documentSize.center;
            Geom.Constrain(ref offset, documentSize.size, state.DraggableBounds);
            return offset - documentSize.center;
        }

        private static void LerpToTarget(Transform transform, Vector3 target, float percent) {
            transform.SetPosition(Vector3.Lerp(transform.localPosition, target, percent), Axis.XY, Space.Self);
            if (Vector2.Distance(transform.localPosition, target) < 0.03f) {
                transform.SetPosition(target, Axis.XY, Space.Self);
            }
        }
    }
}