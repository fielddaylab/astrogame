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
            CanvasSpaceTransformation canvasTransform;
            // canvasTransform.CanvasSpace = 

            foreach (UIFocus focus in m_State.ActiveFocii) {
                // position 2D representation in screen space
                Vector2 point = spaceCam.Camera.Camera.WorldToScreenPoint(focus.Target.transform.position);
                focus.Rect.anchoredPosition = point;
            }

            // position focus outline on currently selected fFocusable, if any
            if (m_State.CurrentFocus) {
                m_State.FocusOutline.enabled = true;
                var point = spaceCam.Camera.Camera.WorldToScreenPoint(m_State.CurrentFocus.Target.transform.position);
                m_State.FocusOutline.rectTransform.anchoredPosition = point;
            }
            else if (m_State.FocusOutline.enabled) {
                m_State.FocusOutline.enabled = false;
            }
        }
    }
}