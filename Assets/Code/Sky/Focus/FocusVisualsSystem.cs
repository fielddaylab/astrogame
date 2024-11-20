using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.LateUpdate)]
    public class FocusVisualsSystem : SharedStateSystemBehaviour<FocusState>
    {
        public override void ProcessWork(float deltaTime)
        {
            foreach (UIFocus focus in m_State.ActiveFocii)
            {
                // position 2D representation in screen space
                var spaceCam = Find.State<SpaceCameraState>();
                var point = spaceCam.Camera.Camera.WorldToScreenPoint(focus.Target.transform.position);
                focus.Rect.anchoredPosition = point;
            }
        }
    }
}