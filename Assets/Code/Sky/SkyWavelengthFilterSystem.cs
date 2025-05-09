using BeauPools;
using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyWavelengthFilterSystem : SharedStateSystemBehaviour<SkyGenerationState>
    {
        public override bool HasWork()
        {
            bool hasWork = base.HasWork();
            if (m_State) {
                hasWork = hasWork && m_State.IsDirty;
            }
            else { 
                return false; 
            }

            return hasWork;
        }

        public override void ProcessWork(float deltaTime)
        {
            var focusState = Find.State<FocusState>();
            var spaceCamera = Find.State<SpaceCameraState>();

            CelestialObjectVisMask visMask = m_State.VisMask;

            foreach(var focus in focusState.ActiveFocii) {
                focus.IsVisibleInCurrentFilter = (focus.TargetData.Visibility & visMask) != 0;
            }

            m_State.IsDirty = false;
            spaceCamera.LookUpdatedThisFrame = true;
        }

        
    }
}