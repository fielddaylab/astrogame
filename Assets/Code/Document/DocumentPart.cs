using FieldDay;
using FieldDay.Components;
using FieldDay.HID;
using FieldDay.UI;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    public class DocumentPart : BatchedComponent, IRegistrationCallbacks {
        public DocumentInteractable Document;
        [NonSerialized] public Collider Collider;
        public DocPartFunction PartType;
        [NonSerialized] public CursorHint Cursor;

        public void OnDeregister() {
            
        }

        public void OnRegister() {
            Collider = GetComponent<Collider>();
            Cursor = GetComponent<CursorHint>();
        }
    }

    public enum DocPartFunction {
        None,
        Zoom,
        Move,
        Close,
        Flip
    }
}