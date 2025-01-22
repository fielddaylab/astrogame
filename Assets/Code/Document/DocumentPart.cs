using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    public class DocumentPart : BatchedComponent, IRegistrationCallbacks {
        public DocumentInteractable Document;
        [NonSerialized] public Collider Collider;
        public DocPartFunction PartType;

        public void OnDeregister() {
            
        }

        public void OnRegister() {
            Collider = GetComponent<Collider>();
        }
    }

    public enum DocPartFunction {
        None,
        Zoom,
        Move,
        Flip
    }
}