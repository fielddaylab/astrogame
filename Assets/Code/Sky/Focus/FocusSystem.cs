using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.Update)]
    public class FocusSystem : ComponentSystemBehaviour<CelestialObject, FocusableObject>
    {
        public override void ProcessWorkForComponent(CelestialObject primary, FocusableObject secondary, float deltaTime)
        {
            var focusState = Find.State<FocusState>();

            // do things according to whether visible on camera
            if (secondary.BecameVisible)
            {
                secondary.BecameVisible = false;
            }
            else if (secondary.BecameInvisible)
            {
                secondary.BecameInvisible = false;
            }
        }
    }
}