using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Filters {
    /// <summary>
    /// Repeat signal tracker.
    /// </summary>
    [Serializable]
    public struct RepeatSignal {

        [Range(0, 1)] public float Analog;
        public RepeatSignalState State;
    }

    /// <summary>
    /// State of a repeat signal.
    /// </summary>
    public enum RepeatSignalState : byte {
        Inactive,
        Active,
        Repeat
    }

    /// <summary>
    /// Attack-repeat timings.
    /// </summary>
    [Serializable]
    public struct RepeatSignalEnvelope {
        public float InitialAttack;
        public float RepeatAttack;

        public RepeatSignalEnvelope(float attack, float repeat) {
            InitialAttack = attack;
            RepeatAttack = repeat;
        }
    }
}