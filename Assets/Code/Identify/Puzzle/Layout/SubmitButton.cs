
using FieldDay;
using FieldDay.Components;
using System;

namespace Astro {

    public class SubmitButton : BatchedComponent, IRegistrationCallbacks {
        public SubmitButtonType ButtonType;

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartPuzzleMode, () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitPuzzle));
            Game.Events.Register(GameEvents.StartOpenMode, () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitIdentification));

        }

        public void OnDeregister() {
            Game.Events.DeregisterAll(GameEvents.StartPuzzleMode);
            Game.Events.DeregisterAll(GameEvents.StartOpenMode);
        }
    }

    [Serializable, Flags]
    public enum SubmitButtonType {
        SubmitPuzzle = 1,
        SubmitIdentification = 2
    }
}