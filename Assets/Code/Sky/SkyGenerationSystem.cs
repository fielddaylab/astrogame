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
                hasWork = hasWork && !m_State.Initialized;
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

            // populate sky with celestial objects
            var center = dome.Position;
            for (int i = 0; i < layout.AllCelestialObjs.Length; i++)
            {
                var newCelestialObj = Instantiate(m_State.CelestialObjPrefab).transform;
                CelestialAsset currAsset = layout.AllCelestialObjs[i];
                // Use UIFocus pool
                var newFocus = focusPools.Focii.Alloc(spaceCamera.Canvas.transform);
                // TODO: assign relevant 2D representation
                FocusableUtility.InitFocusable(focusState, newFocus, newCelestialObj, currAsset, DetermineSprite(currAsset.Category));
                focusState.ActiveFocii.PushBack(newFocus);

                CelestialPositionerUtility.PositionObject(center, newCelestialObj, currAsset.Coords.RightAscension, currAsset.Coords.Declination);
                newCelestialObj.name = currAsset.DisplayName;
            }

            m_State.Initialized = true;
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
                    return null;
                case CelestialObjectCategory.Comet:
                    return null;
                default:
                    return null;
            }
        }
    }
}