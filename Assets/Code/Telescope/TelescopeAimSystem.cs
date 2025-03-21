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
                var spaceCam = Find.State<SpaceCameraState>().Camera.RootTransform;

                TelescopeUtility.UpdateTelescopeRigRotation(m_State, spaceCam);
            }
        }
    }
}
