using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using FieldDay;
using System;
using BeauUtil.Debugger;

namespace Astro {

    public class VirtualScreenRedirector : BaseRaycaster
    {
        [NonSerialized] public Transform screenTransform; // the transform of the screen with the render texture

        [NonSerialized] public Camera eventCameraOverride; // Reference to the camera that views the monitor

        public Camera screenCamera; // Reference to the camera responsible for rendering the virtual screen's rendertexture

        public BaseRaycaster screenCaster; // Reference to the GraphicRaycaster of the canvas displayed on the virtual screen

        private PointerEventData copyEventData;

        public LayerMask RequiredMask = LayerMasks.LabInteract_Mask;

        public override Camera eventCamera { get { return eventCameraOverride; } }

        protected override void Start() {
            base.Start();

            copyEventData = new PointerEventData(EventSystem.current);
            eventCameraOverride = Find.State<ViewState>().Camera.Camera;
            screenTransform = Find.State<MonitorState>().ScreenTransform;
        }

        // Called by Unity when a Raycaster should raycast because it extends BaseRaycaster.
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList) {
            if ((Find.State<InputState>().AppliedLayerMask & RequiredMask) == 0) {
                return;
            }

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

            Ray ray = eventCameraOverride.ScreenPointToRay(copyEventData.position); // Mouse
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {

                if (hit.collider.transform == screenTransform) {
                    Vector2 hitCoord = hit.textureCoord;

                    // Figure out where the pointer would be in the second camera based on texture position or RenderTexture.
                    Vector3 virtualPos = new Vector3(hitCoord.x, hitCoord.y);
                    virtualPos.x *= screenCamera.targetTexture.width;
                    virtualPos.y *= screenCamera.targetTexture.height;

                    copyEventData.position = virtualPos;

                    screenCaster.Raycast(copyEventData, resultAppendList);

                    if (resultAppendList.Count == 0 && copyEventData.button == PointerEventData.InputButton.Left && Input.GetMouseButtonUp(0)) {
                        // Clicked on nothing
                        Game.Events.Dispatch(GameEvents.MonitorEmptySpaceClicked);
                    }

                }
            }
        }
    }
}