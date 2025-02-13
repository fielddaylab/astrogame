using System;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(DocumentRenderer))]
    public sealed class DocumentInteractable : BatchedComponent, IRegistrationCallbacks {
        [HideInInspector] public DocumentRenderer Renderer;
        [HideInInspector] public DocumentPart[] Parts;

        [NonSerialized] public bool Flipped;
        public Transform Paper;

        public void OnDeregister() {
        }

        public void OnRegister() {
            Renderer = GetComponent<DocumentRenderer>();
            Parts = GetComponentsInChildren<DocumentPart>(true);
        }
    }

    public static partial class DocumentUtility {
        static public void SetInteractionLayer(DocumentInteractable interactable, int layer) {
            foreach(var part in interactable.Parts) {
                part.gameObject.layer = layer;
            }
        }
    }
}