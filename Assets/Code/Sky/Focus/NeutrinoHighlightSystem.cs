using FieldDay.Systems;
using FieldDay;
using System;
using UnityEngine;
using BeauRoutine;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.Update, 0, AstroGame.MonitorControlsUpdateMask)]
    public class NeutrinoHighlightSystem : SharedStateSystemBehaviour<NeutrinoHighlightState, FocusState, FocusPools, SkyGenerationState> {
        public override void ProcessWork(float deltaTime) {
            if (m_StateA.OpenModeStarted) {
                if (m_StateD.IsDirty) { return; }

                m_StateA.OpenModeStarted = false;

                PlayerProgressState playerState = Find.State<PlayerProgressState>();
                StoryAsset story = Find.GlobalAsset<StoryAsset>();
                DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[playerState.DayIndex]);

                if (day.NeutrinoEvent.RelevantObjectIds == null) { return; }
                if (m_StateA.SubmissionObjects.Count < 1) { return; }

                // allocate new highlights and assign to relevant focii
                foreach (UIFocus focus in m_StateB.ActiveFocii) {
                    // check if focus is relevant to neutrino
                    if (NeutrinoEventUtil.IsAssetInNeutrinoEvent(focus.TargetData)) {
                        focus.HasHighlight = true;
                        var newHighlight = m_StateC.NeutrinoHighlights.Alloc(focus.Root);
                        focus.Highlight = newHighlight.GetComponent<SpriteRenderer>();

                        focus.Highlight.sprite = focus.IsVisibleInCurrentFilter ? m_StateA.HighlightVisibleSprite : m_StateA.HighlightNotVisibleSprite;
                        focus.Highlight.SetAlpha(focus.IsVisibleInCurrentFilter ? 1 : m_StateA.HighlightNotVisibleAlpha);
                        m_StateA.ActiveHighlights.PushBack(newHighlight);
                    }
                }

                Find.State<SpaceCameraState>().LookUpdatedThisFrame = true;
            }
            
            if (m_StateA.OpenModeEnded) {
                if (m_StateD.IsDirty) { return; }

                m_StateA.OpenModeEnded = false;

                // clear existing highlights
                foreach (var highlight in m_StateA.ActiveHighlights) {
                    if (highlight.parent.TryGetComponent(out UIFocus focus)) {
                        focus.HasHighlight = false;
                        focus.Highlight = null;
                    }
                    m_StateC.NeutrinoHighlights.Free(highlight);
                }

                m_StateA.ActiveHighlights.Clear();
            }
        }
    }
}