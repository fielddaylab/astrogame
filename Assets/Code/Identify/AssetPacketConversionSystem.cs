using Astro;
using BeauUtil;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class AssetPacketConversionSystem : SharedStateSystemBehaviour<DataPacketDistributionState, InstrumentInventoryState>
    {
        private readonly RingBuffer<DataPacket> m_ConvertedPackets = new RingBuffer<DataPacket>(8);
        private DataTypeMask m_AvailableInstrumentTypes;

        public override void ProcessWork(float deltaTime) {
            if (m_StateA.ToConvert == null) { return; }

            // Setup
            
            // Remove any existing packets from distribution queue
            m_StateA.DistributeQueue.Clear();

            // Create a data packet for each type among all available instruments
            m_AvailableInstrumentTypes = 0;
            GatherRelevantDataTypes();
            GenerateNewPackets();

            // Add new packets to the distribution queue
            foreach (var packet in m_ConvertedPackets) {
                m_StateA.DistributeQueue.PushBack(packet);
            }

            // Cleanup
            m_ConvertedPackets.Clear();
            m_StateA.ToConvert = null;
        }

        private void GatherRelevantDataTypes() {
            foreach (var instrument in m_StateB.ActiveInstruments) {
                m_AvailableInstrumentTypes |= InstrumentUtility.GenerateTypeMask(instrument);
            }
        }

        private void GenerateNewPackets()
        {
            if ((m_AvailableInstrumentTypes & DataTypeMask.Name) != 0) {
                DataPacket newPacket = DataPacket.Name(m_StateA.ToConvert);
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Coordinates) != 0) {
                DataPacket newPacket = DataPacket.Coordinates(m_StateA.ToConvert.Coords);
                m_ConvertedPackets.PushBack(newPacket);
            }
            /* TODO: special handling for color
            if ((m_AvailableInstrumentTypes & DataTypeMask.Color) != 0) {
                DataPacket newPacket = DataPacket.Color(m_StateA.ToConvert.ColorId);
                m_ConvertedPackets.PushBack(newPacket);
            }
            */
            if ((m_AvailableInstrumentTypes & DataTypeMask.ApparentMagnitude) != 0) {
                DataPacket newPacket = DataPacket.ApparentMagnitude(m_StateA.ToConvert.ApparentMagnitude);
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.AbsoluteMagnitude) != 0) {
                DataPacket newPacket = DataPacket.AbsoluteMagnitude(m_StateA.ToConvert.AbsoluteMagnitude);
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.MaterialSpectrum) != 0) {
                DataPacket newPacket = DataPacket.Spectrograph(m_StateA.ToConvert.Spectrograph);
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Temperature) != 0) {
                DataPacket newPacket = DataPacket.Temperature(m_StateA.ToConvert.Temperature);
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Distance) != 0) {
                DataPacket newPacket = DataPacket.Distance(m_StateA.ToConvert.Distance);
                m_ConvertedPackets.PushBack(newPacket);
            }
            /* TODO: historical data handling
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_Coordinates) != 0) {
                DataPacket newPacket = DataPacket.HistoricalCoordinates(m_StateA.ToConvert.Coords, );
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_ApparentMagnitude) != 0) {
                DataPacket newPacket = DataPacket.HistoricalApparentMagnitude(m_StateA.ToConvert.ApparentMagnitude, );
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_Temperature) != 0) {
                DataPacket newPacket = DataPacket.HistoricalTemperature(m_StateA.ToConvert.Temperature, );
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_Distance) != 0) {
                DataPacket newPacket = DataPacket.HistoricalDistance(m_StateA.ToConvert.Distance, );
                m_ConvertedPackets.PushBack(newPacket);
            }
            if ((m_AvailableInstrumentTypes & DataTypeMask.Historical_Color) != 0) {
                DataPacket newPacket = DataPacket.HistoricalColor(m_StateA.ToConvert.Color, );
                m_ConvertedPackets.PushBack(newPacket);
            }
            */
        }
    }
}
