using FieldDay.SharedState;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class SpaceCameraState : SharedStateComponent
    {
        // TODO: assign Camera a better way
        public CameraRig Camera;
        public Transform HorizonPlane;
        public bool EnableMouseControls;
        public bool EnableMouseAutoControls;

        [Space(5)]
        [Header("Look")]
        public float LookThreshold; // margin from edge of screen (normalized)
        public float LookRapidThreshold; // faster look threshold
        public float LookSpeed;
        public float LookRapidSpeed;
        public float LookIncrement;
        public Vector2 LookXClamp; // rotation limits in given direction (x is min X, y is max X)
        public Vector2 LookYClamp; // rotation limits in given direction (x is min Y, y is max Y)
        public float LookDragMod;

        [Space(5)]
        [Header("Zoom")]
        public float ZoomSpeed; // scroll wheel
        public float ZoomIncrement; // keyboard

        public Vector2 ZoomBounds; // x is min, y is max

        [HideInInspector] public float VertLook; // accumulated rotation vertically
        [HideInInspector] public float HorizLook; // accumulated rotation horizontally

        [HideInInspector] public bool MouseDragLookActive;
        [HideInInspector] public Vector3 PrevMousePos;
    }

    public static class SpaceCameraUtility
    {
        public static void TryLook(Vector3 lookPos, Transform camRoot, Transform orientRoot)
        {
            camRoot.LookAt(lookPos, orientRoot.up);
            var angles = camRoot.localEulerAngles;
            angles.x = 0;
            camRoot.localEulerAngles = angles;
        }
    }
}