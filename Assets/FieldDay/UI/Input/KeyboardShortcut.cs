using FieldDay.Components;
using FieldDay.HID;
using UnityEngine;

namespace FieldDay.UI {
    public sealed class KeyboardShortcut : BatchedComponent {
        public KeyCode KeyCode;
        public ModifierKeyCode Modifiers;

        // TODO: Handle alternate combos?
        // TODO: Handle clicking
    }
}