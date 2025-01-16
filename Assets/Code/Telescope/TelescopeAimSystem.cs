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
                var rig = Find.State<SpaceCameraState>().Camera.RootTransform;

                animator.AimPivot.localRotation = rig.rotation;
                //animator.BasePivot.localEulerAngles = new Vector3(0, rig.localEulerAngles.y, 0);
            }
        }
    }
}
