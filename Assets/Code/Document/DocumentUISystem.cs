using UnityEngine;
using FieldDay.Systems;

namespace Astro {
    public class DocumentUISystem : ComponentSystemBehaviour<IconUI> {

        public override void ProcessWorkForComponent(IconUI element, float deltaTime) {
            if(element.IsHighlighted) {
                element.Renderer.sharedMaterial = element.HoverMaterial;
            } else {
                element.Renderer.sharedMaterial = element.BaseMateral;
            }
        }
    }
}
