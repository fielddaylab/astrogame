using BeauUtil;
using FieldDay.SharedState;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class DataPacketDistributionState : SharedStateComponent
    {
        [NonSerialized] public CelestialAsset ToConvert;
        [NonSerialized] public bool ReadyToConvert;
        public RingBuffer<DataPacket> DistributeQueue = new RingBuffer<DataPacket>(16);
    }

    public static class DataDistributionUtility
    {
        public static void QueueConversion(DataPacketDistributionState state, CelestialAsset toConvert)
        {
            // Current implementation only needs to handle 1 selected object at a time
            state.ToConvert = toConvert;
            state.ReadyToConvert = true;
        }
    }
}