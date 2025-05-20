using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 10)]
    public class TelescopeAimSystem : SharedStateSystemBehaviour<TelescopeRig>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            if (m_State.AutoSync)
            {
                SpaceCameraState cam = Find.State<SpaceCameraState>();
                var spaceCam = cam.Camera.RootTransform;

                if (cam.LookUpdatedThisFrame) {
                    TelescopeUtility.UpdateTelescopeRigRotation(m_State, spaceCam);
                }
            }
        }
    }
}
