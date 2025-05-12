using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;
using BeauUtil;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 100, AstroGame.InteractUpdateMask)] // AFter FocusVisualsSystem
    public class FocusTargetSystem : SharedStateSystemBehaviour<FocusState>
    {
        public override void ProcessWork(float deltaTime)
        {
            var spaceCam = Find.State<SpaceCameraState>();

            // position focus outline on currently selected Focusable, if any
            if (m_State.FocusUpdated) {
                m_State.FocusUpdated = false;
                if (m_State.CurrentFocus) {
                    m_State.FocusOutline.enabled = true;
                    FocusVisualsUtility.AlignFocusOutlineToTarget(spaceCam, m_State);
                }
                else if (m_State.FocusOutline.enabled) {
                    m_State.FocusOutline.enabled = false;
                }
            }
        }
    }
}