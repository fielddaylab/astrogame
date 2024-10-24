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

            // populate sky with celestial objects
            var center = dome.Position;
            for (int i = 0; i < layout.AllCelestialObjs.Length; i++)
            {
                var newCelestialObj = Instantiate(m_State.CelestialObjPrefab).transform;
                CelestialAsset currAsset = layout.AllCelestialObjs[i];
                CelestialPositionerUtility.PositionObject(center, newCelestialObj, currAsset.Coords.RightAscension, currAsset.Coords.Declination);
                newCelestialObj.name = currAsset.DisplayName;
            }

            m_State.Initialized = true;
        }
    }
}