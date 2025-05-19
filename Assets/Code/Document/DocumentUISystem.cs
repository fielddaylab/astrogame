using UnityEngine;
using FieldDay.Systems;

namespace Astro {
    public class DocumentUISystem : ComponentSystemBehaviour<DocumentUI> {

        public override void ProcessWorkForComponent(DocumentUI element, float deltaTime) {
            if(element.IsHighlighted) {
                element.Renderer.sharedMaterial = element.HoverMaterial;
            } else {
                element.Renderer.sharedMaterial = element.BaseMateral;
            }
        }
    }
}
