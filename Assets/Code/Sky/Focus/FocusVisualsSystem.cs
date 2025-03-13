using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.LateUpdate)]
    public class FocusVisualsSystem : SharedStateSystemBehaviour<FocusState, NeutrinoHighlightState>
    {
        public override void ProcessWork(float deltaTime)
        {
            var spaceCam = Find.State<SpaceCameraState>();

            if (spaceCam.LookUpdatedThisFrame) {
                foreach (UIFocus focus in m_StateA.ActiveFocii) {
                    if (focus.TargetRenderer.isVisible) {
                        focus.Represent2D.enabled = true;
                        focus.Button.enabled = true;
                        // position 2D representation in screen space
                        Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(focus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                        focus.Rect.anchorMin = focus.Rect.anchorMax = viewPoint;

                        // TODO: Highlight neutrino-relevant objects with special focus
                        if (focus.NeutrinoHighlight) {

                        }
                    }
                    else {
                        focus.Represent2D.enabled = false;
                        focus.Button.enabled = false;
                    }
                }

                if (m_StateA.CurrentFocus) {
                    AlignFocusOutlineToTarget(spaceCam);
                }
            }

            // position focus outline on currently selected Focusable, if any
            if (m_StateA.FocusUpdated) {
                m_StateA.FocusUpdated = false;
                if (m_StateA.CurrentFocus) {
                    m_StateA.FocusOutline.enabled = true;
                    AlignFocusOutlineToTarget(spaceCam);
                }
                else if (m_StateA.FocusOutline.enabled) {
                    m_StateA.FocusOutline.enabled = false;
                }
            }
        }

        private void AlignFocusOutlineToTarget(SpaceCameraState spaceCam) {
            Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(m_StateA.CurrentFocus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
            m_StateA.FocusOutline.rectTransform.anchorMin = m_StateA.FocusOutline.rectTransform.anchorMax = viewPoint;
        }
    }
}