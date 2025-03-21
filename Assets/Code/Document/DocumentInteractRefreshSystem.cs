using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.Systems;

namespace Astro
{
    [SysUpdate(GameLoopPhase.LateUpdate)]
    public class DocumentInteractRefreshSystem : SharedStateSystemBehaviour<DocumentBoardState>
    {
        public override void ProcessWork(float deltaTime)
        {
            base.ProcessWork(deltaTime);

            m_State.DraggablePlacedThisFrame = false;
            m_State.DraggablePlaced = null;
        }
    }

}