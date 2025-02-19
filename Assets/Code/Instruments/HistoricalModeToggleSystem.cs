using FieldDay.Systems;

namespace Astro {
    public class HistoricalModeToggleSystem : ComponentSystemBehaviour<HistoricalModeToggle, LabInteractable> {
        public override void ProcessWorkForComponent(HistoricalModeToggle primary, LabInteractable secondary, float deltaTime) {
            if (!secondary.InteractReceived) { return; }
            HistoricalDataUtility.ToggleInstrumentMode();
        }
    }
}