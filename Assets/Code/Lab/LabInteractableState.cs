using System;
using UnityEngine;

using FieldDay;
using FieldDay.HID;
using FieldDay.SharedState;

namespace Astro {
    public class LabInteractableState : SharedStateComponent {
        public CursorHint TapCursorLock;

        [NonSerialized] public LabInteractable CurrInteractable;
        [NonSerialized] public Vector2 StartMousePos;
        [NonSerialized] public Vector2 CurrMousePos;
        [NonSerialized] public int BlockInteractions;
    }

    static public class LabInteractableUtility {
        static public bool ReleaseCurrentInteractable() {
            LabInteractableState state = Find.State<LabInteractableState>();
            if (state.CurrInteractable) {
                CursorHint.Unlock(state.CurrInteractable.Cursor);
                state.CurrInteractable.IsDragging = false;
                state.CurrInteractable.InteractEnded = true;
                state.CurrInteractable = null;
                state.StartMousePos = state.CurrMousePos = Vector2.zero;
                return true;
            }

            return false;
        }
    }
}