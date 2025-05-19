using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using UnityEngine;

namespace Astro {
    /// <summary>
    /// Utility class for positioning objects a given latitude and longitude location on earth.
    /// This affects the stars visible in the night sky by virtue of positioning.
    /// </summary>
    public static class WorldPositionUtility
    {
        public static void LookAt(SpaceCameraState camState, EqCoords coords)
        {
            float raDegrees = (float)CoordinateUtility.HmsToDD(coords.RightAscension);
            float declDegrees = (float)CoordinateUtility.DmsToDD(coords.Declination);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);
            posOffset = camState.Camera.RootTransform.parent.InverseTransformDirection(posOffset);

            Quaternion look = Quaternion.LookRotation(posOffset, Vector3.up);
            Vector3 angles = look.eulerAngles;
            angles.x = MathUtils.Wrap(angles.x, -180, 180);
            angles.y = MathUtils.Wrap(angles.y, -180, 180);

            Vector3 clampedAngles = angles;
            clampedAngles.z = 0;
            clampedAngles.y = SpaceCameraUtility.ClampAngle(clampedAngles.y, camState.LookXClamp.x, camState.LookXClamp.y);
            clampedAngles.x = SpaceCameraUtility.ClampAngle(clampedAngles.x, camState.LookYClamp.x, camState.LookYClamp.y);
            if (clampedAngles.x != angles.x || clampedAngles.y != angles.y) {
                Log.Warn("[WorldPositionUtility] LookAt resulted in look rotation outside normal clamping bounds: {0},{1}", clampedAngles.y, clampedAngles.x);
            }

            camState.Camera.RootTransform.localEulerAngles = clampedAngles;

            camState.HorizLook = clampedAngles.y;
            camState.VertLook = clampedAngles.x;

            camState.LookUpdatedThisFrame = true;
            camState.OnLookUpdated.Invoke(camState);
        }

        public static Vector3 GetLookVector(EqCoords coords) {
            float raDegrees = (float)CoordinateUtility.HmsToDD(coords.RightAscension);
            float declDegrees = (float)CoordinateUtility.DmsToDD(coords.Declination);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);

            return posOffset.normalized;
        }

        public static Quaternion GetLocalLookRotation(SpaceCameraState camState, EqCoords coords) {
            float raDegrees = (float)CoordinateUtility.HmsToDD(coords.RightAscension);
            float declDegrees = (float)CoordinateUtility.DmsToDD(coords.Declination);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);
            posOffset = camState.Camera.RootTransform.parent.InverseTransformDirection(posOffset);

            Quaternion look = Quaternion.LookRotation(posOffset, Vector3.up);
            Vector3 angles = look.eulerAngles;
            angles.z = 0;

            return Quaternion.Euler(angles);
        }

        public static void ForceLocalRotation(SpaceCameraState camState, Vector3 angles) {

            angles.x = MathUtils.Wrap(angles.x, -180, 180);
            angles.y = MathUtils.Wrap(angles.y, -180, 180);
            angles.z = 0;
            camState.Camera.RootTransform.localEulerAngles = angles;

            camState.HorizLook = angles.y;
            camState.VertLook = angles.x;

            camState.LookUpdatedThisFrame = true;
            camState.OnLookUpdated.Invoke(camState);
        }

        public static void UpdateLookCoordinatesFromCurrentLook(SpaceCameraState camState) {
            Vector3 angles = camState.Camera.RootTransform.localEulerAngles;
            angles.x = MathUtils.Wrap(angles.x, -180, 180);
            angles.y = MathUtils.Wrap(angles.y, -180, 180);
            angles.z = 0;

            camState.HorizLook = angles.y;
            camState.VertLook = angles.x;
            camState.LookUpdatedThisFrame = true;
            camState.OnLookUpdated.Invoke(camState);
        }
    }
}
