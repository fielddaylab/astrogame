
using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.LateUpdate, 0)] // After trigger processing systems
    public class DialRefreshSystem : ComponentSystemBehaviour<InteractAdjustDial> {
        public override void ProcessWorkForComponent(InteractAdjustDial component, float deltaTime) {
            component.ValChanged = false;
            component.ConstrainedValDelta = 0;
            component.InteractEnded = false;
            component.InteractReceived = false;
        }
    }
}