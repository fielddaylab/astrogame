using FieldDay;
using FieldDay.SharedState;

namespace Astro {
    public class InputState : SharedStateComponent {
        public bool InputEnabled;
    }

    public static class InputUtility {
        public static void SetInputEnabled(bool enabled) {
            SpaceCameraUtility.SetCameraInputEnabled(enabled);
            if (enabled) {
                Game.Input.ResumeRaycasts();
            } else {
                Game.Input.PauseRaycasts();
            }
        }
    }

}