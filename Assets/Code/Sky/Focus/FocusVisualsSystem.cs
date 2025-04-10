using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 0)]
    public class FocusVisualsSystem : SharedStateSystemBehaviour<FocusState>
    {
        public override void ProcessWork(float deltaTime)
        {
            var spaceCam = Find.State<SpaceCameraState>();

            if (spaceCam.LookUpdatedThisFrame) {
                foreach (UIFocus focus in m_State.ActiveFocii) {
                    if (focus.TargetRenderer.isVisible) {
                        focus.Represent2D.enabled = true;
                        focus.Button.enabled = true;
                        // position 2D representation in screen space
                        Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(focus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                        focus.Rect.anchorMin = focus.Rect.anchorMax = viewPoint;
                        focus.HighlightRect.gameObject.SetActive(true);
                    }
                    else {
                        focus.Represent2D.enabled = false;
                        focus.Button.enabled = false;
                        focus.HighlightRect.gameObject.SetActive(false);
                    }
                }

                if (m_State.CurrentFocus) {
                    FocusVisualsUtility.AlignFocusOutlineToTarget(spaceCam, m_State);
                }
            }
        }
    }

    public static class FocusVisualsUtility
    {
        public static void AlignFocusOutlineToTarget(SpaceCameraState spaceCam, FocusState state)
        {
            Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(state.CurrentFocus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
            state.FocusOutline.rectTransform.anchorMin = state.FocusOutline.rectTransform.anchorMax = viewPoint;
        }
    }
}