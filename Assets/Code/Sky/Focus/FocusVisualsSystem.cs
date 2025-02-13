using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.LateUpdate)]
    public class FocusVisualsSystem : SharedStateSystemBehaviour<FocusState>
    {
        public override void ProcessWork(float deltaTime)
        {
            var spaceCam = Find.State<SpaceCameraState>();

            if (!spaceCam.LookUpdatedThisFrame) { return; }

            foreach (UIFocus focus in m_State.ActiveFocii) {
                if (focus.TargetRenderer.isVisible) {
                    focus.Represent2D.enabled = true;
                    focus.Button.enabled = true;
                    // position 2D representation in screen space
                    Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(focus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                    focus.Rect.anchorMin = focus.Rect.anchorMax = viewPoint;
                }
                else {
                    focus.Represent2D.enabled = false;
                    focus.Button.enabled = false;
                }
            }

            // position focus outline on currently selected Focusable, if any
            if (m_State.CurrentFocus) {
                m_State.FocusOutline.enabled = true;
                Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(m_State.CurrentFocus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                m_State.FocusOutline.rectTransform.anchorMin = m_State.FocusOutline.rectTransform.anchorMax = viewPoint;
            }
            else if (m_State.FocusOutline.enabled) {
                m_State.FocusOutline.enabled = false;
            }
        }
    }
}