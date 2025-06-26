using FieldDay;
using FieldDay.SharedState;

namespace Astro {
    public sealed class TitleState : SharedStateComponent, IRegistrationCallbacks {
        public bool LockNodeChanges;

        void IRegistrationCallbacks.OnDeregister() {
            Game.Events.DeregisterAllForContext(this);
        }

        void IRegistrationCallbacks.OnRegister() {
            Game.Events.Register(GameEvents.TitleGameStarting, () => {
                LockNodeChanges = true;
            });
        }
    }
}