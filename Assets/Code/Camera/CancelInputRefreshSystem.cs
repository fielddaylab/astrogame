using Astro;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class CancelInputRefreshSystem : SharedStateSystemBehaviour<CancelInputState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            m_State.ClickedThisFrame = false;
            m_State.SlotClicked = false;
        }
    }
}
