using BeauPools;
using BeauUtil;
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
            focusState.ActiveFociiVisibleBits.Clear();

            CelestialObjectVisMask visMask = m_State.VisMask;

            Vector3 camPos = spaceCamera.Camera.RootTransform.position;
            Vector3 camUp = spaceCamera.Camera.RootTransform.up;

            spaceCamera.StarRoot.position = camPos;

            int fociiAllocated = 0;
            foreach(var obj in dome.AboveHorizon) {
                if ((obj.Resource.Visibility & visMask) == 0) {
                    continue;
                }

                var newFocus = focusPools.Focii.Alloc(spaceCamera.StarRoot);
                // TODO: assign relevant 2D representation
                FocusableUtility.InitFocusable(focusState, newFocus, obj.transform, obj.Resource, DetermineSprite(obj.Resource.Category));
                focusState.ActiveFocii.PushBack(newFocus);

                UIFocusPackedData packed;
                packed.TargetPos = obj.transform.position;
                packed.TargetVector = Vector3.Normalize(packed.TargetPos - camPos);

                focusState.ActiveFociiPacked[fociiAllocated++] = packed;

                newFocus.Root.SetLocalPositionAndRotation(packed.TargetVector * 20, Quaternion.LookRotation(-packed.TargetVector, camUp));
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