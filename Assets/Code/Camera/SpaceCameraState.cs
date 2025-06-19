using System;
using BeauUtil;
using BeauRoutine;
using Leaf.Runtime;
using FieldDay;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public class SpaceCameraState : SharedStateComponent, IRegistrationCallbacks {
        // TODO: assign Camera a better way
        public CameraRig Camera;
        public Skybox Skybox;
        public Transform HorizonPlane;
        public bool EnableMouseControls;
        public bool EnableMouseAutoControls;
        public bool EnableSmoothKeyboardControls = true;
        public bool InputEnabled = true;

        public Transform StarRoot;

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
        //TODO: [NonSerialized] 
        public float Zoom = 1;

        [NonSerialized] public bool MouseDragLookActive;
        [NonSerialized] public Vector3 PrevMousePos;

        [NonSerialized] public ulong StateHash;

        [NonSerialized] public bool ZoomInputLocked = false;
        [NonSerialized] public bool CameraRotationInputLocked = false;
        [NonSerialized] public bool LookUpdatedThisFrame = false;

        public CastableEvent<SpaceCameraState> OnLookUpdated = new CastableEvent<SpaceCameraState>();

        public void OnRegister() {
            Game.Events.Register(GameEvents.StartPuzzleNavigation, SpaceCameraUtility.OnStartPuzzleNav);
            Game.Events.Register(GameEvents.StopPuzzleNavigation, SpaceCameraUtility.OnStopPuzzleNav);
            Game.Events.Register(GameEvents.StopPuzzleMode, SpaceCameraUtility.OnStopPuzzleMode);
        }

        public void OnDeregister() {}

        public enum DebuggingFlags {
            DisplayLookCoords
        }
    }

    public static class SpaceCameraUtility {
        public static void OnStartPuzzleNav() {
            SpaceCameraState spaceCameraState = Find.State<SpaceCameraState>();
            spaceCameraState.ZoomInputLocked = true;

            PuzzleState puzzleState = Find.State<PuzzleState>();
            
            float newFOV = spaceCameraState.Camera.OriginalFOV / puzzleState.ActivePuzzle.PuzzleCameraZoom;
            Camera spaceCamera = spaceCameraState.Camera.Camera;

            Routine.Start(spaceCameraState, 
                Tween.Float(spaceCamera.fieldOfView, newFOV, (f) => {
                    spaceCamera.fieldOfView = f;                    
                    spaceCameraState.LookUpdatedThisFrame = true;
                    spaceCameraState.OnLookUpdated.Invoke(spaceCameraState);
                }, 1f)
            );
        }

        public static void OnStopPuzzleNav() {
            SpaceCameraState spaceCameraState = Find.State<SpaceCameraState>();
            spaceCameraState.CameraRotationInputLocked = true;
            SetCameraInputEnabled(false);
        }

        public static void OnStopPuzzleMode() {
            SpaceCameraState spaceCameraState = Find.State<SpaceCameraState>();
            spaceCameraState.ZoomInputLocked = false;
        }

        public static void TryLook(Vector3 lookPos, Transform camRoot, Transform orientRoot) {
            camRoot.LookAt(lookPos, orientRoot.up);
            var angles = camRoot.localEulerAngles;
            angles.x = 0;
            camRoot.localEulerAngles = angles;
        }

        [LeafMember("TelescopeLook")]
        public static void LeafAdjustLook(float deltaHoriz, float deltaVert) {
            SpaceCameraState cam = Find.State<SpaceCameraState>();
            cam.HorizLook += deltaHoriz;
            ClampAngle(cam.HorizLook, cam.LookXClamp.x, cam.LookXClamp.y);
            cam.VertLook += deltaVert ;
            ClampAngle(cam.VertLook, cam.LookYClamp.x, cam.LookYClamp.y);

            var angles = cam.Camera.RootTransform.localEulerAngles;
            angles.x = cam.VertLook;
            angles.y = cam.HorizLook;
            cam.Camera.RootTransform.localEulerAngles = angles;
            cam.OnLookUpdated.Invoke(cam);
            cam.LookUpdatedThisFrame = true;
            TelescopeUtility.SuppressTelescopeRigAudio(true);
        }

        public static void SetCameraInputEnabled(bool enabled) {
            if (Game.SharedState.TryGet(out SpaceCameraState camState)) {
                camState.InputEnabled = enabled;
            }
        }

        public static float ClampAngle(float lfAngle, float lfMin, float lfMax) {
            if (lfAngle < -360f) {
                lfAngle += 360f;
            }
            if (lfAngle > 360f) {
                lfAngle -= 360f;
            }

            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }
    }
}