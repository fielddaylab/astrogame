
using FieldDay;
using FieldDay.Components;
using System;

namespace Astro {

    public class SubmitButton : BatchedComponent, IRegistrationCallbacks {
        public SubmitButtonType ButtonType;

        private Action setButtonToPuzzle;  
        private Action setButtonToId;  

        public void OnRegister() {
            setButtonToPuzzle = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitPuzzle);
            setButtonToId = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitIdentification);

            Game.Events.Register(GameEvents.StartPuzzleMode, setButtonToPuzzle);
            Game.Events.Register(GameEvents.StartOpenMode, () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitIdentification));
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartPuzzleMode, setButtonToPuzzle);
            Game.Events.Deregister(GameEvents.StartOpenMode, setButtonToId);
        }
    }

    [Serializable, Flags]
    public enum SubmitButtonType {
        SubmitPuzzle = 1,
        SubmitIdentification = 2
    }
}