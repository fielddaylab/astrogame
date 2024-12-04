using System;

namespace Astro {
    [Serializable]    
    public enum TimerBehavior {
        Loop,
        PauseOnPeriod,
    }

    [Serializable]
    public struct Timer {
        public float Period;
        public TimerBehavior Behavior;
        [NonSerialized] public bool Paused;
        [NonSerialized] public float Accumulator;
        [NonSerialized] public bool AdvancedRecently;

        public Timer(float period, TimerBehavior behavior) {
            Period = period;
            Behavior = behavior;
            Paused = false;
            Accumulator = 0;
            AdvancedRecently = false;
        }

        /// <summary>
        /// Accumulates time and returns true if period has advanced
        /// </summary>
        /// <param name="deltaTime">Time to add the the Period</param>
        /// <returns>True if the Period has been exceeded since last check</returns>
        public bool Advance(float deltaTime) {
            if (Paused) return false;
            Accumulator += deltaTime;
            if (Accumulator >= Period) {
                AdvancedRecently = true;
                switch (Behavior) {
                    case TimerBehavior.Loop: {
                        Accumulator -= Period;
                        break;
                    }
                    case TimerBehavior.PauseOnPeriod: {
                        Accumulator = 0;
                        Paused = true;
                        break;
                    }
                    default: {
                        break;
                    }
                }
                return true;
            } else {
                AdvancedRecently = false;
                return false;
            }
        }

        public readonly float GetProgress() {
            return Accumulator / Period;
        }

        public void Reset() {
            Accumulator = 0;
        }
    }
}