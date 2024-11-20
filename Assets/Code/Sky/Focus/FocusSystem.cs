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

            // reveal or hide according to visible on camera
            if (secondary.BecameVisible)
            {
                // focusState.ActiveFocii.PushBack(secondary);
                // ent2D.enabled = false;
                // TODO: map from focusable to focusState.ActiveFocii?
                secondary.BecameVisible = false;
            }
            else if (secondary.BecameInvisible)
            {
                // secondary.Represent2D.enabled = false;
                secondary.BecameInvisible = false;
            }
        }
    }
}