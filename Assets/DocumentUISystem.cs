using UnityEngine;
using FieldDay.Systems;

namespace Astro {
    public class DocumentUISystem : ComponentSystemBehaviour<DocumentUI> {

        public override void ProcessWorkForComponent(DocumentUI element, float deltaTime) {
            if(element.IsHighlighted) {
                element.Renderer.material = element.HoverMaterial;
            } else {
                element.Renderer.material = element.BaseMateral;
            }
        }
    }
}
