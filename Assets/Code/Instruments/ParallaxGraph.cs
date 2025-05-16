using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(HistoricalDataGraph))]
    public class ParallaxGraph : BatchedComponent, IRegistrationCallbacks {
        public DataDisplay DistanceDisplay;
        public HistoricalDataGraph Graph;
        public void OnDeregister() {
        }

        public void OnRegister() {
            DistanceDisplay.OnDisplayRequested.Register(
                (packet, flags) => ParallaxUtility.OnDisplayRequest(this, packet, flags));
            DistanceDisplay.OnDisplayCleared.Register(
                () => ParallaxUtility.OnDisplayClear(this));
        }

        public static class ParallaxUtility {
            public static void OnDisplayRequest(ParallaxGraph parallax, DataPacket packet, DataFormattingFlags flags) {
                HistoricalDataState hds = Find.State<HistoricalDataState>();
                if (hds.SendingAbsMag && (packet.Type & DataTypeMask.Distance) != 0) {
                    HistoricalDataUtility.SetParallaxScale(parallax.Graph, packet, hds);
                }
            }

            public static void OnDisplayClear(ParallaxGraph parallax) {
                HistoricalDataUtility.ClearPattern(parallax.Graph);
            }
        }
    }
}