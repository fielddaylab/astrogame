using BeauUtil;
using FieldDay;
using FieldDay.SharedState;
using System;
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
        public bool EnableSmoothKeyboardControls = true;
        public bool InputEnabled = true;

        public Canvas Canvas;

        [Space(5)]
        [Header("Look")]
        public float LookThreshold; // margin from edge of screen (normalized)
        public float LookRapidThreshold; // faster look threshold
        public float LookSpeed;
        public float LookRapidSpeed;
        public float LookIncrement;
        public float SmoothLookIncrement;
        public Vector2 LookXClamp; // rotation limits in given direction (x is min X, y is max X)
        public Vector2 LookYClamp; // rotation limits in given direction (x is min Y, y is max Y)
        public float LookDragMod;

        [Space(5)]
        [Header("Zoom")]
        public float ZoomSpeed; // scroll wheel
        public float ZoomIncrement; // keyboard

        public Vector2 ZoomBounds; // x is min, y is max

        [NonSerialized] public float VertLook; // accumulated rotation vertically
        [NonSerialized] public float HorizLook; // accumulated rotation horizontally
        [NonSerialized] public float Zoom = 1;

        [NonSerialized] public bool MouseDragLookActive;
        [NonSerialized] public Vector3 PrevMousePos;

        [NonSerialized] public ulong StateHash;

        [NonSerialized] public bool LookUpdatedThisFrame = false;

        public CastableEvent<SpaceCameraState> OnLookUpdated = new CastableEvent<SpaceCameraState>();
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

        public static void SetCameraInputEnabled(bool enabled) {
            Find.State<SpaceCameraState>().InputEnabled = enabled;
        }
    }
}