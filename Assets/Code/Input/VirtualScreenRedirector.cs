using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.PointerEventData;
using UnityEngine.UIElements;
using FieldDay;

namespace Astro {

    public class VirtualScreenRedirector : GraphicRaycaster
    {
        public Transform screenTransform; // the transform of the screen with the render texture

        public Camera screenCamera; // Reference to the camera responsible for rendering the virtual screen's rendertexture

        public GraphicRaycaster screenCaster; // Reference to the GraphicRaycaster of the canvas displayed on the virtual screen

        private PointerEventData copyEventData = new PointerEventData(EventSystem.current);

        // Called by Unity when a Raycaster should raycast because it extends BaseRaycaster.
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            copyEventData = new PointerEventData(EventSystem.current);
            copyEventData.eligibleForClick = false;

            copyEventData.pointerId = eventData.pointerId;
            copyEventData.position = eventData.position;
            copyEventData.delta = eventData.delta;
            copyEventData.pressPosition = eventData.pressPosition;
            copyEventData.clickTime = eventData.clickTime;
            copyEventData.clickCount = eventData.clickCount;

            copyEventData.scrollDelta = eventData.scrollDelta;
            copyEventData.useDragThreshold = eventData.useDragThreshold;
            copyEventData.dragging = eventData.dragging;
            copyEventData.button = eventData.button;

            copyEventData.pressure = eventData.pressure;
            copyEventData.tangentialPressure = eventData.tangentialPressure;
            copyEventData.altitudeAngle = eventData.altitudeAngle;
            copyEventData.azimuthAngle = eventData.azimuthAngle;
            copyEventData.twist = eventData.twist;
            copyEventData.radius = eventData.radius;
            copyEventData.radiusVariance = eventData.radiusVariance;

            Ray ray = eventCamera.ScreenPointToRay(copyEventData.position); // Mouse
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                // Debug.Log("[VirtualScreen] raycast hit");

                if (hit.collider.transform == screenTransform)
                {
                    // Figure out where the pointer would be in the second camera based on texture position or RenderTexture.
                    Vector3 virtualPos = new Vector3(hit.textureCoord.x, hit.textureCoord.y);
                    virtualPos.x *= screenCamera.targetTexture.width;
                    virtualPos.y *= screenCamera.targetTexture.height;

                    copyEventData.position = virtualPos;

                    screenCaster.Raycast(copyEventData, resultAppendList);

                    if (Input.GetMouseButtonUp(0) && resultAppendList.Count == 0) {
                        // Clicked on nothing
                        Game.Events.Dispatch(GameEvents.MonitorEmptySpaceClicked);
                    }

                    // Debug.Log("[VirtualScreen] redirected to " + copyEventData.position);
                }
                else
                {
                    //Debug.Log("[VirtualScreen] hit but not screen transform");
                }
            }
            else
            {
                // Debug.Log("[VirtualScreen] default cast to " + copyEventData.position);
                // base.Raycast(copyEventData, resultAppendList);
            }
        }
    }
}