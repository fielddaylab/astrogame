
using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;

namespace Astro {

    public class SubmitButton : BatchedComponent, IRegistrationCallbacks {
        public SubmitButtonType ButtonType;
        public GameObject Root;

        private Action m_SetButtonToPuzzle;  
        private Action m_SetButtonToId;  

        public void OnRegister() {
            m_SetButtonToPuzzle = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitPuzzle);
            m_SetButtonToId = () => PuzzleUtility.SetButtonMode(this, SubmitButtonType.SubmitIdentification);

            Game.Events.Register(GameEvents.StartPuzzleMode, m_SetButtonToPuzzle);
            Game.Events.Register(GameEvents.StartOpenMode, m_SetButtonToId);
        }

        public void OnDeregister() {
            Game.Events.Deregister(GameEvents.StartPuzzleMode, m_SetButtonToPuzzle);
            Game.Events.Deregister(GameEvents.StartOpenMode, m_SetButtonToId);
        }
    }

    [Serializable, Flags]
    public enum SubmitButtonType {
        SubmitPuzzle = 1,
        SubmitIdentification = 2
    }
}