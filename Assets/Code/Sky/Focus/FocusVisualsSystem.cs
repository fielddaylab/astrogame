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

            // TODO: only update on frames where the camera has changed its look vector
            foreach (UIFocus focus in m_State.ActiveFocii) {
                // position 2D representation in screen space
                Vector2 viewPoint = spaceCam.Camera.Camera.WorldToViewportPoint(focus.Target.position, Camera.MonoOrStereoscopicEye.Mono);
                focus.Rect.anchorMin = focus.Rect.anchorMax = viewPoint;

                // TODO: disable offscreen focii (dot product above certain threshold)
                // TODO: maybe stagger that? but that also interacts strangely with only updating
                //      on dirty frames, so consider further
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