using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhaseMask.LateUpdate, 0, AstroGame.InstrumentUpdateMask)]
    public class DataTransferRefreshSystem : SharedStateSystemBehaviour<DataTransferState>
    {
        public override void ProcessWork(float deltaTime)
        {
            m_State.SourceUpdated = false;
        }
    }
}

