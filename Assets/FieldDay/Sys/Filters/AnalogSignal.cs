using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace FieldDay.Filters {
    /// <summary>
    /// Analog signal tracker.
    /// </summary>
    [Serializable]
    public struct AnalogSignal {
        [Range(0, 1)] public float Analog;
        public bool Digital;

        /// <summary>
        /// Processes a frame of digital input.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Process(ref AnalogSignal signal, bool digitalState, float deltaTime, in SignalLatchWindow window, in SignalEnvelope ad) {
            if (digitalState) {
                return Activate(ref signal, deltaTime, window, ad.Attack);
            } else {
                return Deactivate(ref signal, deltaTime, window, ad.Decay);
            }
        }

        /// <summary>
        /// Processes a frame of positive input.
        /// </summary>
        static public bool Activate(ref AnalogSignal signal, float deltaTime, in SignalLatchWindow window, float attack) {
            if (signal.Analog < 1) {
                signal.Analog = Math.Min(signal.Analog + deltaTime / attack, 1);
                if (!signal.Digital && signal.Analog >= window.OnThreshold) {
                    signal.Digital = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Processes a frame of negative input.
        /// </summary>
        static public bool Deactivate(ref AnalogSignal signal, float deltaTime, in SignalLatchWindow window, float decay) {
            if (signal.Analog > 0) {
                signal.Analog = Math.Max(signal.Analog - deltaTime / decay, 0);
                if (signal.Digital && signal.Analog <= window.OffThreshold) {
                    signal.Digital = false;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Forces the signal to a single digital state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public bool Reset(ref AnalogSignal signal, bool digitalState) {
            signal.Analog = digitalState ? 1 : 0;
            bool prevDigital = signal.Digital;
            signal.Digital = digitalState;
            return prevDigital != digitalState;
        }
    }

    /// <summary>
    /// Signal latching window.
    /// </summary>
    [Serializable]
    public struct SignalLatchWindow {
        /// <summary>
        /// Value beyond which the signal's digital value is set to true.
        /// </summary>
        [Range(0, 1)] public float OnThreshold;

        /// <summary>
        /// Value beyond which the signal's digital value is set to false.
        /// </summary>
        [Range(0, 1)] public float OffThreshold;

        public SignalLatchWindow(float onThreshold, float offThreshold) {
            OnThreshold = onThreshold;
            OffThreshold = offThreshold;
        }

        public SignalLatchWindow(float threshold) {
            OnThreshold = threshold;
            OffThreshold = threshold;
        }

        /// <summary>
        /// Default latching threshold.
        /// </summary>
        static public readonly SignalLatchWindow Default = new SignalLatchWindow(0.5f);

        /// <summary>
        /// Full latching threshold.
        /// </summary>
        static public readonly SignalLatchWindow Full = new SignalLatchWindow(1, 0);
    }

    /// <summary>
    /// Attack-decay timings.
    /// </summary>
    [Serializable]
    public struct SignalEnvelope {
        public float Attack;
        public float Decay;

        public SignalEnvelope(float attack, float decay) {
            Attack = attack;
            Decay = decay;
        }
    }
}