using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 10)]
    public class TelescopeAimSystem : SharedStateSystemBehaviour<TelescopeRig> {
        public override void ProcessWork(float deltaTime) {
            base.ProcessWork(deltaTime);
            if (!m_State.AutoSync) return;

            SpaceCameraState cam = Find.State<SpaceCameraState>();
            var spaceCam = cam.Camera.RootTransform;

            if (cam.LookUpdatedThisFrame) {
                TelescopeUtility.UpdateTelescopeRigRotation(m_State, spaceCam);
            }
        }
    }
}
