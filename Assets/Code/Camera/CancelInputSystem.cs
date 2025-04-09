using Astro;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    /// <summary>
    /// Tracks when nothing in particular is clicked so that various systems may behave accordingly,
    /// such as by cancelling focus.
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 1000, AstroGame.InteractUpdateMask)] // After InteractSelectSlotSystem, before SlotEffectSystem
    public class CancelInputSystem : SharedStateSystemBehaviour<CancelInputState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            if (Input.GetMouseButtonDown(0)) {
                m_State.ClickedThisFrame = true;
            }

            if (m_State.ClickedThisFrame && !m_State.SlotClicked) {
                // clicked on nothing in particular
                // cancel data slot transfer states
                DataUtility.ClearSelections(Find.State<DataTransferState>());
            }
        }
    }
}
