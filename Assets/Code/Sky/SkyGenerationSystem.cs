using BeauPools;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SkyGenerationSystem : SharedStateSystemBehaviour<SkyGenerationState>
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
            var layout = Find.GlobalAsset<SkyLayoutAsset>();
            var dome = Find.State<SkyDome>();
            var focusPools = Find.State<FocusPools>();
            var focusState = Find.State<FocusState>();
            var spaceCamera = Find.State<SpaceCameraState>();

            focusPools.Focii.Free(focusState.ActiveFocii);

            CelestialObjectVisMask visMask = m_State.VisMask;

            foreach(var obj in dome.AllObjects) {
                if ((obj.Resource.Visibility & visMask) == 0) {
                    continue;
                }

                var newFocus = focusPools.Focii.Alloc(spaceCamera.Canvas.transform);
                // TODO: assign relevant 2D representation
                FocusableUtility.InitFocusable(focusState, newFocus, obj.transform, obj.Resource, DetermineSprite(obj.Resource.Category));
                focusState.ActiveFocii.PushBack(newFocus);
            }

            m_State.IsDirty = false;
            spaceCamera.LookUpdatedThisFrame = true;
        }

        private Sprite DetermineSprite(CelestialObjectCategory category)
        {
            switch(category)
            {
                case CelestialObjectCategory.Star:
                    return m_State.DefaultStarSprite;
                case CelestialObjectCategory.Planet:
                    return m_State.DefaultPlanetSprite;
                case CelestialObjectCategory.Satellite:
                    return null;
                case CelestialObjectCategory.Constellation:
                    return null;
                case CelestialObjectCategory.Galaxy:
                    return m_State.DefaultGalaxySprite;
                case CelestialObjectCategory.Comet:
                    return null;
                default:
                    return null;
            }
        }
    }
}