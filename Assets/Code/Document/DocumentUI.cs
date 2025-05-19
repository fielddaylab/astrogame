using UnityEngine;
using FieldDay.Components;

namespace Astro {
    public class DocumentUI : BatchedComponent {
        public MeshRenderer Renderer;
        public Collider Collider;

        public Material BaseMateral;
        public Material HoverMaterial;

        [HideInInspector] public bool IsHighlighted = false;

        void OnMouseEnter() { IsHighlighted = true; }
        void OnMouseExit() { IsHighlighted = false; }
        void OnMouseDown() { IsHighlighted = false; }
    }
}