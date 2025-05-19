using FieldDay.Systems;
using FieldDay;
using System;

namespace Astro {
    [SysUpdate(GameLoopPhaseMask.Update, 0, AstroGame.MonitorControlsUpdateMask)]
    public class NeutrinoHighlightSystem : SharedStateSystemBehaviour<NeutrinoHighlightState, FocusState, FocusPools, SkyGenerationState>
    {
        public override void ProcessWork(float deltaTime)
        {
            if (m_StateA.OpenModeStarted)
            {
                if (m_StateD.IsDirty) { return; }

                m_StateA.OpenModeStarted = false;

                PlayerProgressState playerState = Find.State<PlayerProgressState>();
                StoryAsset story = Find.GlobalAsset<StoryAsset>();
                DayConfigAsset day = Find.NamedAsset<DayConfigAsset>(story.Days[playerState.DayIndex]);

                if (day.NeutrinoEvent.RelevantObjectIds == null) { return; }

                // allocate new highlights and assign to relevant focii
                foreach (UIFocus focus in m_StateB.ActiveFocii)
                {
                    // check if focus is relevant to neutrino
                    if (Array.IndexOf(day.NeutrinoEvent.RelevantObjectIds, focus.TargetData.AssetId) != -1) {
                        focus.HasHighlight = true;
                        var newHighlight = m_StateC.NeutrinoHighlights.Alloc(focus.Root);
                        m_StateA.ActiveHighlights.PushBack(newHighlight);
                    }
                }

            }
            if (m_StateA.OpenModeEnded)
            {
                if (m_StateD.IsDirty) { return; }

                m_StateA.OpenModeEnded = false;

                // clear existing highlights
                foreach (var highlight in m_StateA.ActiveHighlights) {
                    if (highlight.parent.TryGetComponent(out UIFocus focus)) {
                        focus.HasHighlight = false;
                    }
                    m_StateC.NeutrinoHighlights.Free(highlight);
                }

                m_StateA.ActiveHighlights.Clear();
            }
        }
    }
}