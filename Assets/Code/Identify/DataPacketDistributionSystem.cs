using BeauUtil;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    [SysUpdate(GameLoopPhase.Update, 0, AstroGame.InstrumentUpdateMask)]
    public class DataPacketDistributionSystem : SharedStateSystemBehaviour<DataPacketDistributionState, InstrumentInventoryState>
    {
        private readonly RingBuffer<DataPacket> m_DistributeWorkList = new RingBuffer<DataPacket>(8);

        public override void ProcessWork(float deltaTime)
        {
            // setup
            m_StateA.DistributeQueue.CopyTo(m_DistributeWorkList);
            m_StateA.DistributeQueue.Clear();

            // Distribution packets to relevant data slots
            foreach (var packet in m_DistributeWorkList) {
                // Instrument data slots
                if (!m_StateB.ActiveInstrumentMap.ContainsKey(packet.Type)) {
                    Debug.LogWarning("[Packet Distribution] A data packet was created, but there were no corresponding instruments!");
                    continue;
                }

                foreach (var instrument in m_StateB.ActiveInstrumentMap[packet.Type]) {
                    foreach (var slot in instrument.AutoPopulated) {
                        DataUtility.TrySetData(slot, packet);
                    }
                }
            }

            // cleanup
            m_DistributeWorkList.Clear();

            m_DistributeWorkList.CopyTo(m_StateA.DistributeQueue);
        }
    }
}

