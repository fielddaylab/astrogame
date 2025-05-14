using System;
using BeauUtil;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Astro {
    // [RequireComponent(typeof(DocumentRenderer))]
    public sealed class DocumentInteractable : BatchedComponent {
        [HideInInspector] public DocumentRenderer Renderer;
        [HideInInspector] public DocumentPart[] Parts;

        [NonSerialized] public StringHash32 AssetName;

        [NonSerialized] public bool Flipped;
        [NonSerialized] public bool IsDragging;
        public Transform BodyRoot;

        private void Awake() {
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