using BeauPools;
using BeauRoutine;
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
            var neutrinoState = Find.State<NeutrinoHighlightState>();

            CelestialObjectVisMask visMask = m_State.VisMask;

            bool prevVis;
            foreach(var focus in focusState.ActiveFocii) {
                prevVis = focus.IsVisibleInCurrentFilter;
                focus.IsVisibleInCurrentFilter = (focus.TargetData.Visibility & visMask) != 0;

                if (focus.HasHighlight && focus.IsVisibleInCurrentFilter != prevVis) {
                    focus.Highlight.sprite = focus.IsVisibleInCurrentFilter ? neutrinoState.HighlightVisibleSprite : neutrinoState.HighlightNotVisibleSprite;
                    focus.Highlight.SetAlpha(focus.IsVisibleInCurrentFilter ? 1 : neutrinoState.HighlightNotVisibleAlpha);
                }
            }

            m_State.IsDirty = false;
            spaceCamera.LookUpdatedThisFrame = true;
        }

        
    }
}