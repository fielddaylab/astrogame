
using FieldDay.Components;
using System;

namespace Astro {

    public class SubmitButton : BatchedComponent {
        public SubmitButtonType ButtonType;
    }

    [Serializable]
    public enum SubmitButtonType {
        SubmitPuzzle,
        SubmitIdentification
    }
}