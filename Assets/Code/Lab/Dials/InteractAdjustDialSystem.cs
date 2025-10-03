using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 10)] // After MouseInteractionSystem
    public class InteractAdjustDialSystem : ComponentSystemBehaviour<InteractAdjustDial, LabInteractable> {
        public override void ProcessWorkForComponent(InteractAdjustDial primary, LabInteractable secondary, float deltaTime) {
            if (secondary.InteractReceived) {
                primary.InteractReceived = true;
            }
            if (secondary.InteractEnded) {
                primary.BaseVal = primary.CurrConstrainedVal;
            }

            if (!secondary.IsDragging) { return; }

            var interactState = Find.State<LabInteractableState>();
            var delta = interactState.CurrMousePos - interactState.StartMousePos;

            if (primary.CanAdjust == null || primary.CanAdjust.Invoke(primary, delta)) {
                DialUtility.TryAdjustDial(primary, delta.x);
            } else {
                LabInteractableUtility.ReleaseCurrentInteractable();
            }

            if (secondary.InteractEnded) {
                primary.InteractEnded = true;
            }
        }
    }
}
