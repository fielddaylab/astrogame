using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Filters {
    /// <summary>
    /// Repeat signal tracker.
    /// </summary>
    [Serializable]
    public struct RepeatSignal {
        public bool Digital;
        public RepeatSignalState State;
        [Range(0, 1)] public float Analog;

        /// <summary>
        /// Processes an input signal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Process(ref RepeatSignal signal, bool input, float deltaTime, in RepeatSignalEnvelope envelope) {
            if (!input) {
                signal.Digital = false;
                signal.State = RepeatSignalState.Inactive;
                signal.Analog = 0;
                return false;
            }

            switch(signal.State) {
                case RepeatSignalState.Inactive: {
                    signal.Digital = true;
                    signal.Analog = 0;
                    signal.State = RepeatSignalState.Initial;
                    break;
                }

                default: {
                    signal.Analog += deltaTime / (signal.State == RepeatSignalState.Initial ? envelope.InitialAttack : envelope.RepeatAttack);
                    if (signal.Analog >= 1) {
                        signal.Digital = true;
                        signal.State = RepeatSignalState.Repeat;
                        signal.Analog -= 1;
                    } else {
                        signal.Digital = false;
                    }
                    break;
                }
            }

            return signal.Digital;
        }

        /// <summary>
        /// Forces the signal to a single digital state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void Reset(ref RepeatSignal signal) {
            signal.Analog = 0;
            signal.Digital = false;
            signal.State = RepeatSignalState.Inactive;
        }
    }

    /// <summary>
    /// State of a repeat signal.
    /// </summary>
    public enum RepeatSignalState : byte {
        Inactive,
        Initial,
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