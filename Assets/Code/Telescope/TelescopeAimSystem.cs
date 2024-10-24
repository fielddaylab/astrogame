using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.FixedUpdate)]
    public class TelescopeAimSystem : ComponentSystemBehaviour<TelescopeAnimator>
    {
        public override void ProcessWorkForComponent(TelescopeAnimator animator, float deltaTime)
        {
            if (animator.AutoSync) {
                var rig = Find.State<SpaceCameraRig>();

                animator.AimPivot.transform.localRotation = rig.transform.rotation;
            }
        }
    }
}
