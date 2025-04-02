

using FieldDay;
using FieldDay.Systems;
using UnityEngine;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 502, AstroGame.SubmissionUpdateMask)] // After SubmitButtonSystem
    public class RefGuideToggleSystem : ComponentSystemBehaviour<RefGuideToggle, LabInteractable> {
        public override void ProcessWorkForComponent(RefGuideToggle primary, LabInteractable secondary, float deltaTime) {
            if (!secondary.InteractReceived) { return; }
            ReferenceUtility.ToggleReferenceActive();
        }
    }
}