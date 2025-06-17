using FieldDay.Systems;
using FieldDay;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 0)]
    public class SpaceCameraRefreshSystem : SharedStateSystemBehaviour<SpaceCameraState> {
        public override void ProcessWork(float deltaTime) {
            base.ProcessWork(deltaTime);
            m_State.LookUpdatedThisFrame = false;
        }
    }
}