using FieldDay;
using UnityEngine;

namespace Astro {
    /// <summary>
    /// Utility class for positioning objects a given latitude and longitude location on earth.
    /// This affects the stars visible in the night sky by virtue of positioning.
    /// </summary>
    public static class WorldPositionUtility
    {
        public static void TryLook(SpaceCameraState camState, EqCoords coords)
        {
            float raDegrees = (float)CoordinateUtility.HmsToDD(coords.RightAscension);
            float declDegrees = (float)CoordinateUtility.DmsToDD(coords.Declination);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);
            posOffset = camState.Camera.RootTransform.parent.InverseTransformDirection(posOffset);

            Quaternion look = Quaternion.LookRotation(posOffset, Vector3.up);
            Vector3 angles = look.eulerAngles;
            angles.z = 0;
            camState.Camera.RootTransform.localEulerAngles = angles;

            camState.HorizLook = angles.y;
            camState.VertLook = angles.x;

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

            angles.z = 0;
            camState.Camera.RootTransform.localEulerAngles = angles;

            camState.HorizLook = angles.y;
            camState.VertLook = angles.x;

            camState.OnLookUpdated.Invoke(camState);
        }
    }
}
