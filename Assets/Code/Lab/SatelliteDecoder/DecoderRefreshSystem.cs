using Astro;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.LateUpdate, 0, AstroGame.InteractUpdateMask)] // After trigger processing systems
    public class DecoderRefreshSystem : SharedStateSystemBehaviour<SatelliteDecoderState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            m_State.InputUpdatedThisFrame = false;
        }
    }
}