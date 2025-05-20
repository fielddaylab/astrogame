using BeauUtil;
using FieldDay;
using FieldDay.Systems;

namespace Astro {
    /// <summary>
    /// Handles the conversion of selected Celestial Assets into their corrisponding DataPackets to be passed to DataPacketDistributionSystem.
    /// </summary>
    [SysUpdate(GameLoopPhase.Update, 9999)]
    public class AssetPacketConversionSystem : SharedStateSystemBehaviour<DataPacketDistributionState, InstrumentInventoryState> {
        private readonly RingBuffer<DataPacket> m_ConvertedPackets = new RingBuffer<DataPacket>(16);
        private DataTypeMask m_AvailableInstrumentTypes;

        public override void ProcessWork(float deltaTime) {
            if (!m_StateA.ReadyToConvert) { return; }

            // Setup
            
            // Remove any existing packets from distribution queue
            m_StateA.DistributeQueue.Clear();

            // Create a data packet for each type among all available instruments
            m_AvailableInstrumentTypes = GatherRelevantDataTypes();
            GenerateNewPackets();

            // Add new packets to the distribution queue
            foreach (var packet in m_ConvertedPackets) {
                m_StateA.DistributeQueue.PushBack(packet);
            }

            // Cleanup
            m_ConvertedPackets.Clear();
            m_StateA.ToConvert = null;
            m_StateA.ReadyToConvert = false;
        }

        private DataTypeMask GatherRelevantDataTypes() {
            DataTypeMask relevantDataTypes = 0;
            foreach (var instrument in m_StateB.ActiveInstruments) {
                relevantDataTypes |= InstrumentUtility.GenerateTypeMask(instrument);
            }
            return relevantDataTypes;
        }

        /// <summary>
        /// Queue a DataPacket based on the currently selected celesital asset to be passed to the DataPacketDistributionSystem
        /// </summary>
        private void GenerateNewPackets() {
            if ((m_AvailableInstrumentTypes & DataTypeMask.Name) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Name(m_StateA.ToConvert);
                }
                else {
                    newPacket = DataPacket.Null(DataTypeMask.Name);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Coordinates) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Coordinates(m_StateA.ToConvert.Coords);
                }
                else {
                    newPacket = DataPacket.Null(DataTypeMask.Coordinates);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }

            // Photometer packets first
            if ((m_AvailableInstrumentTypes & DataTypeMask.ApparentMagnitude) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null && ((m_StateA.ToConvert.Visibility & CelestialObjectVisMask.Visible) != 0 || m_StateA.ToConvert.ApparentMagnitude != 0)) {
                    newPacket = DataPacket.ApparentMagnitude(m_StateA.ToConvert.ApparentMagnitude);
                }
                else {
                    newPacket = DataPacket.MinAppMagnitude();
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.BlueMagnitude) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null && ((m_StateA.ToConvert.Visibility & CelestialObjectVisMask.Blue) != 0 || m_StateA.ToConvert.ApparentBlueMagnitude != 0)) {
                    newPacket = DataPacket.BlueMagnitude(m_StateA.ToConvert.ApparentBlueMagnitude);
                } else {
                    newPacket = DataPacket.MinBlueMagnitude();
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.InfraredMagnitude) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null && (m_StateA.ToConvert.Visibility & CelestialObjectVisMask.Infrared) != 0) {
                    newPacket = DataPacket.InfraredMagnitude(m_StateA.ToConvert.ApparentIRMagnitude);
                } else {
                    newPacket = DataPacket.MinIRMagnitude();
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.AbsoluteMagnitude) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null && ((m_StateA.ToConvert.Visibility & CelestialObjectVisMask.Visible) != 0 || m_StateA.ToConvert.AbsoluteMagnitude != 0)) {
                    newPacket = DataPacket.AbsoluteMagnitude(m_StateA.ToConvert.AbsoluteMagnitude);
                }
                else {
                    newPacket = DataPacket.MinAbsMagnitude();
                }
                m_ConvertedPackets.PushBack(newPacket);
            }

            // Color packets second, to handle their dependency on photometer packets
            if ((m_AvailableInstrumentTypes & DataTypeMask.Color) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Color(m_StateA.ToConvert.ColorId);
                } else {
                    newPacket = DataPacket.Null(DataTypeMask.Color);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.ColorIndex) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null && !m_StateA.ToConvert.ColorId.IsEmpty) {
                    newPacket = DataPacket.ColorIndex(m_StateA.ToConvert.ApparentBlueMagnitude - m_StateA.ToConvert.ApparentMagnitude);
                } else {
                    newPacket = DataPacket.Null(DataTypeMask.ColorIndex);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }

            // and then everything else, no dependencies
            if ((m_AvailableInstrumentTypes & DataTypeMask.MaterialSpectrum) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Spectrograph(m_StateA.ToConvert.Spectrograph);
                }
                else {
                    newPacket = DataPacket.Null(DataTypeMask.MaterialSpectrum);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Temperature) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Temperature(m_StateA.ToConvert.Temperature);
                }
                else {
                    newPacket = DataPacket.Null(DataTypeMask.Temperature);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Distance) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.Distance(m_StateA.ToConvert.Distance);
                }
                else {
                    newPacket = DataPacket.Null(DataTypeMask.Distance);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                DataPacket newPacket;
                if (m_StateA.ToConvert != null) {
                    newPacket = DataPacket.HistoricalApparentMagnitude(m_StateA.ToConvert.HistoricalBrightness);
                } else {
                    newPacket = DataPacket.Null(DataTypeMask.Historical_ApparentMagnitude);
                }
                m_ConvertedPackets.PushBack(newPacket);
            }
        }
    }
}
