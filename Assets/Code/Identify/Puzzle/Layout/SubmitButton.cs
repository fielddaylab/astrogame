
using FieldDay.Components;
using System;

namespace Astro {

    public class SubmitButton : BatchedComponent {
        public SubmitButtonType ButtonType;
    }

    [Serializable, Flags]
    public enum SubmitButtonType {
        SubmitPuzzle = 1,
        SubmitIdentification = 2
    }
}