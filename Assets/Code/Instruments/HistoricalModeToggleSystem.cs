using FieldDay;
using FieldDay.Systems;

namespace Astro {
    [SysUpdate(GameLoopPhase.Update, 4, AstroGame.InstrumentUpdateMask)] // After RowSelectSystem
    public class HistoricalModeToggleSystem : ComponentSystemBehaviour<HistoricalModeToggle, LabInteractable> {
        public override void ProcessWorkForComponent(HistoricalModeToggle primary, LabInteractable secondary, float deltaTime) {
            if (!secondary.InteractReceived) { return; }
            ParallaxDataUtility.ToggleInstrumentMode();
        }
    }
}