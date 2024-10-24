using System;
using System.Collections;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class CameraRig : BatchedComponent {
        [Header("Components")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera;
        public CameraFOVPlane FOVPlane;
        public Transform RootTransform;
        public Transform EffectsTransform;

        public CameraFOVMode Mode;
        public Routine TransitionRoutine;

#if UNITY_EDITOR

        private void OnValidate() {
            if (Application.IsPlaying(this)) {
                return;
            }

            Mode = FOVPlane != null && FOVPlane.enabled ? CameraFOVMode.Plane : CameraFOVMode.Direct;
        }

#endif // UNITY_EDITOR
    }

    public enum CameraFOVMode {
        Plane,
        Direct
    }

    public struct CameraRigState {
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView;
        public float Height;
        public float Zoom;

        public CameraRigState(Vector3 pos, Quaternion rot, float height, float zoom, float fov) {
            Position = pos;
            Rotation = rot;
            Height = height;
            Zoom = zoom;
            FieldOfView = fov;
        }

        public CameraRigState(CameraRig rig) {
            rig.RootTransform.GetLocalPositionAndRotation(out Position, out Rotation);
            FieldOfView = rig.Camera.fieldOfView;
            if (rig.FOVPlane) {
                Height = rig.FOVPlane.Height;
                Zoom = rig.FOVPlane.Zoom;
            } else {
                Height = 0;
                Zoom = 1;
            }
        }

        public CameraRigState(CameraPose pose) {
            pose.CacheComponent(ref pose.CachedTransform).GetPositionAndRotation(out Position, out Rotation);
            FieldOfView = pose.FieldOfView;
            Height = pose.Height;
            Zoom = pose.Zoom;
        }

        static public void Lerp(in CameraRigState a, in CameraRigState b, ref CameraRigState output, float lerp) {
            output.Position = Vector3.LerpUnclamped(a.Position, b.Position, lerp);
            output.Rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, lerp);
            output.Height = Mathf.LerpUnclamped(a.Height, b.Height, lerp);
            output.Zoom = Mathf.LerpUnclamped(a.Zoom, b.Zoom, lerp);
            output.FieldOfView = Mathf.LerpUnclamped(a.FieldOfView, b.FieldOfView, lerp);
        }

        static public void Lerp<T>(in CameraRigState a, in CameraRigState b, ref CameraRigState output, float lerp, T posSpline)
            where T : ISpline {
            output.Position = posSpline.GetPoint(lerp);
            output.Rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, lerp);
            output.Height = Mathf.LerpUnclamped(a.Height, b.Height, lerp);
            output.Zoom = Mathf.LerpUnclamped(a.Zoom, b.Zoom, lerp);
            output.FieldOfView = Mathf.LerpUnclamped(a.FieldOfView, b.FieldOfView, lerp);
        }
    }

    static public class CameraRigUtility {
        /// <summary>
        /// Moves the camera to the given pose.
        /// </summary>
        static public IEnumerator MoveToPose(CameraRig rig, CameraPose pose, float duration, Curve curve = Curve.Smooth) {
            if (rig.FOVPlane != null && pose.Target) {
                rig.FOVPlane.SetTargetPreserveFOV(pose.Target);
            }

            CameraRigState state = new CameraRigState(rig);
            CameraRigState newState = new CameraRigState(pose);

            rig.Mode = pose.Mode;
            if (rig.FOVPlane != null) {
                rig.FOVPlane.enabled = pose.Mode == CameraFOVMode.Plane;
            }

            if (duration <= 0) {
                ApplyStateToRig(newState, rig);
                rig.TransitionRoutine.Stop();
                return null;
            }

            rig.TransitionRoutine.Replace(rig, MoveRigTween(rig, state, newState, new TweenSettings(duration, curve)));
            return rig.TransitionRoutine.Wait();
        }

        /// <summary>
        /// Moves the camera to the given pose, incorporating a control point for a position spline.
        /// </summary>
        static public IEnumerator MoveToPoseWithControlPoint(CameraRig rig, CameraPose pose, Vector3 controlPoint, float duration, Curve curve = Curve.Smooth) {
            if (rig.FOVPlane != null && pose.Target) {
                rig.FOVPlane.SetTargetPreserveFOV(pose.Target);
            }

            CameraRigState state = new CameraRigState(rig);
            CameraRigState newState = new CameraRigState(pose);

            rig.Mode = pose.Mode;
            if (rig.FOVPlane != null) {
                rig.FOVPlane.enabled = pose.Mode == CameraFOVMode.Plane;
            }

            if (duration <= 0) {
                ApplyStateToRig(newState, rig);
                rig.TransitionRoutine.Stop();
                return null;
            }

            rig.TransitionRoutine.Replace(rig, MoveRigSplineTween(rig, state, controlPoint, newState, new TweenSettings(duration, curve)));
            return rig.TransitionRoutine.Wait();
        }

        /// <summary>
        /// Applies the given state to the given rig.
        /// </summary>
        static public void ApplyStateToRig(in CameraRigState state, CameraRig rig) {
            rig.RootTransform.SetLocalPositionAndRotation(state.Position, state.Rotation);
            rig.Camera.fieldOfView = state.FieldOfView;
            if (rig.FOVPlane) {
                rig.FOVPlane.Height = state.Height;
                rig.FOVPlane.Zoom = state.Zoom;
            }
        }

        #region Routines

        static private IEnumerator MoveRigTween(CameraRig rig, CameraRigState start, CameraRigState target, TweenSettings tween) {
            return Tween.ZeroToOne((f) => {
                CameraRigState newState = default;
                CameraRigState.Lerp(start, target, ref newState, f);
                ApplyStateToRig(newState, rig);
            }, tween);
        }

        static private IEnumerator MoveRigSplineTween(CameraRig rig, CameraRigState start, Vector3 controlPoint, CameraRigState target, TweenSettings tween) {
            SimpleSpline spline = new SimpleSpline(start.Position, target.Position, controlPoint);
            return Tween.ZeroToOne((f) => {
                CameraRigState newState = default;
                CameraRigState.Lerp(start, target, ref newState, f, spline);
                ApplyStateToRig(newState, rig);
            }, tween);
        }

        #endregion // Routines
    }
}