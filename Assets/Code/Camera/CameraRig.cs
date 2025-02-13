using System;
using System.Collections;
using BeauRoutine;
using BeauRoutine.Splines;
using BeauUtil;
using FieldDay;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class CameraRig : BatchedComponent, IRegistrationCallbacks {
        [Header("Components")]
        [Required(ComponentLookupDirection.Children)] public Camera Camera;
        public Transform RootTransform;
        public Transform EffectsTransform;

        public Routine TransitionRoutine;

        [NonSerialized] public float OriginalFOV = -1;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            if (OriginalFOV < 0) {
                OriginalFOV = Camera.fieldOfView;
            }
        }

#if UNITY_EDITOR

        private void OnValidate() {
            if (Application.IsPlaying(this)) {
                return;
            }
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

        public CameraRigState(Vector3 pos, Quaternion rot, float fov) {
            Position = pos;
            Rotation = rot;
            FieldOfView = fov;
        }

        public CameraRigState(CameraRig rig) {
            rig.RootTransform.GetLocalPositionAndRotation(out Position, out Rotation);
            FieldOfView = rig.Camera.fieldOfView;
        }

        public CameraRigState(CameraPose pose) {
            pose.CacheComponent(ref pose.CachedTransform).GetPositionAndRotation(out Position, out Rotation);
            FieldOfView = pose.FieldOfView;
        }

        static public void Lerp(in CameraRigState a, in CameraRigState b, ref CameraRigState output, float lerp) {
            output.Position = Vector3.LerpUnclamped(a.Position, b.Position, lerp);
            output.Rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, lerp);
            output.FieldOfView = Mathf.LerpUnclamped(a.FieldOfView, b.FieldOfView, lerp);
        }

        static public void Lerp<T>(in CameraRigState a, in CameraRigState b, ref CameraRigState output, float lerp, T posSpline)
            where T : ISpline {
            output.Position = posSpline.GetPoint(lerp);
            output.Rotation = Quaternion.SlerpUnclamped(a.Rotation, b.Rotation, lerp);
            output.FieldOfView = Mathf.LerpUnclamped(a.FieldOfView, b.FieldOfView, lerp);
        }
    }

    static public class CameraRigUtility {
        /// <summary>
        /// Moves the camera to the given pose.
        /// </summary>
        static public IEnumerator MoveToPose(CameraRig rig, CameraPose pose, float duration, Curve curve = Curve.Smooth) {
            CameraRigState state = new CameraRigState(rig);
            CameraRigState newState = new CameraRigState(pose);

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
            CameraRigState state = new CameraRigState(rig);
            CameraRigState newState = new CameraRigState(pose);

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