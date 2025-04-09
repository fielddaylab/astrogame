using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.SubmissionUpdateMask)]
    public class ReferenceRenderRefreshSystem : SharedStateSystemBehaviour<RefGuideRenderState> {
        public override void ProcessWork(float deltaTime) {
            if (m_State.RenderNeedsRefresh) {
                m_State.RefRenderCam.enabled = true;
                m_State.RenderNeedsRefresh = false;
            } else if (m_State.RefRenderCam.enabled) {
                m_State.RefRenderCam.enabled = false;
            }
        }
    }
}