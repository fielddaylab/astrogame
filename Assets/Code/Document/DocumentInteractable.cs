using System;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(DocumentRenderer))]
    public sealed class DocumentInteractable : BatchedComponent, IRegistrationCallbacks {
        [NonSerialized] public DocumentRenderer Renderer;
        [NonSerialized] public bool Flipped;
        public Transform Paper;

        public void OnDeregister() {
        }

        public void OnRegister() {
            Renderer = GetComponent<DocumentRenderer>();
        }
    }
}