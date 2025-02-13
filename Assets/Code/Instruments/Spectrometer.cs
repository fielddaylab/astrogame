

using Astro;
using FieldDay;
using FieldDay.Components;


namespace Astro {
    public class Spectrometer : BatchedComponent, IRegistrationCallbacks {
        public DataDisplay Display;
        public Spectrograph Graphic;

        public void OnRegister() {
            Display.OnDisplayRequested.Register(
                (packet, flags) => SpectrographUtility.OnDisplayRequest(this, packet, flags));
            Display.OnDisplayCleared.Register(
                () => SpectrographUtility.OnDisplayClear(this));
        }
        public void OnDeregister() {
        }
    }

    public static partial class SpectrographUtility {
        public static void OnDisplayRequest(Spectrometer spec, DataPacket packet, DataFormattingFlags flags) {
            SetMaterials(spec.Graphic, packet.Value.Materials);
        }
        public static void OnDisplayClear(Spectrometer spec) {
            ClearMaterials(spec.Graphic);
        }
    }
}


