using FieldDay;
using FieldDay.Scripting;
using FieldDay.SharedState;
using FieldDay.Systems;

namespace Astro {
    public sealed class ScriptSkipState : SharedStateComponent {
        public enum KeyState {
            Unheld,
            Pressed,
            Held
        }

        public KeyState State;
        public float Timer;
    }
}