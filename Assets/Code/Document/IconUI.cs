using UnityEngine;
using FieldDay.Components;
using UnityEngine.EventSystems;

namespace Astro {
    public class IconUI : BatchedComponent, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler {
        public MeshRenderer Renderer;
        public Collider Collider;

        public Material BaseMateral;
        public Material HoverMaterial;

        [HideInInspector] public bool IsHighlighted = false;

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
            IsHighlighted = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            IsHighlighted = false;
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
            IsHighlighted = false;
        }
    }
}