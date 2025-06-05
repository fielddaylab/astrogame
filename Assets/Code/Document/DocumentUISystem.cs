using UnityEngine;
using FieldDay.Systems;
using FieldDay;

namespace Astro {
    public class DocumentUISystem : ComponentSystemBehaviour<IconUI> {

        public override void ProcessWorkForComponent(IconUI element, float deltaTime) {
            if (!Find.State<InputState>().InputEnabled || Game.Input.AreRaycastsPaused()) {
                element.Renderer.sharedMaterial = element.BaseMateral;
                return;
            }

            if(element.IsHighlighted) {
                element.Renderer.sharedMaterial = element.HoverMaterial;
            } else {
                element.Renderer.sharedMaterial = element.BaseMateral;
            }
        }
    }
}
