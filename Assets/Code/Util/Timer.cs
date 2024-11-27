using System;

namespace Astro {
    public struct Timer {
        public float Period;
        [NonSerialized] public bool Paused;
        [NonSerialized] public float Accumulator;
        [NonSerialized] public bool AdvancedRecently;

        public Timer(float period) {
            Period = period;
            Paused = false;
            Accumulator = 0;
            AdvancedRecently = false;
        }

        /// <summary>
        /// Small struct for accumulating time
        /// </summary>
        /// <param name="deltaTime">Time to add the the Period</param>
        /// <returns>True if the Period has been exceeded since last check</returns>
        public bool Advance(float deltaTime) {
            if (Paused) return false;
            Accumulator += deltaTime;
            if (Accumulator >= Period) {
                AdvancedRecently = true;
                return true;
            } else {
                AdvancedRecently = false;
                return false;
            }
        }

        public float GetProgress() {
            return Accumulator / Period;
        }
    }
}