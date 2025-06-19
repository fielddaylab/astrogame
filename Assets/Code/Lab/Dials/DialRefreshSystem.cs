using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    [SysUpdate(GameLoopPhase.LateUpdate, 0)] // After trigger processing systems
    public class DialRefreshSystem : ComponentSystemBehaviour<InteractAdjustDial>
    {
        public override void ProcessWorkForComponent(InteractAdjustDial component, float deltaTime)
        {
            component.ValChanged = false;
            component.ConstrainedValDelta = 0;
        }
    }
}