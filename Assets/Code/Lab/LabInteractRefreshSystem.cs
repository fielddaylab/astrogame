using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro
{
    [SysUpdate(GameLoopPhase.LateUpdate, 0)] // After trigger processing systems
    public class LabInteractRefreshSystem : ComponentSystemBehaviour<LabInteractable>
    {
        public override void ProcessWorkForComponent(LabInteractable component, float deltaTime)
        {
            component.InteractReceived = false;
            component.InteractEnded = false;
        }
    }
}