using Astro;
using FieldDay;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Astro
{
    public class WorldPositionSystem : SharedStateSystemBehaviour<WorldPositionState, SpaceCameraState>
    {
        public override void ProcessWork(float deltaTime)
        {
            if (m_StateA.Initialized) { return; }

            WorldPositionUtility.TryLook(m_StateB.HorizonPlane, m_StateB.Camera.RootTransform, m_StateA.StartingLookCoords);

            m_StateA.Initialized = true;
        }
    }

    /// <summary>
    /// Utility class for positioning objects a given latitude and longitude location on earth.
    /// This affects the stars visible in the night sky by virtue of positioning.
    /// </summary>
    public static class WorldPositionUtility
    {
        public static void PositionAtLatLongHms(Transform toPosition, Transform relativeTo, Transform plane, HmsCoords lat, HmsCoords longitude)
        {
            float latDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
               lat.Hours,
               lat.Minutes,
               lat.Seconds);

            float longDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
                longitude.Hours,
                longitude.Minutes,
                longitude.Seconds);

            PositionAtLatLongDegrees(toPosition, relativeTo, plane, latDegrees, longDegrees, 2);
        }

        public static void PositionAtLatLongDegrees(Transform toPosition, Transform relativeTo, Transform plane, float latDegrees, float longDegrees, float heightOffset)
        {
            var pos = CoordinateUtility.LatLongToCartesianCoordinates(latDegrees, longDegrees);

            pos *= (relativeTo.localScale.x / 2);
            pos.y += heightOffset * Mathf.Sign(pos.y);

            toPosition.position = pos;

            // Calculate the direction from this object to the target
            Vector3 directionToTarget = (relativeTo.position - toPosition.position).normalized;

            // Create a rotation that points the object's negative Y-axis (bottom) at the target
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);

            // Apply the rotation to the object
            toPosition.rotation = targetRotation;

            Vector3 dirToPlayer;

            if (plane)
            {
                // Calculate the direction from this object to the target
                dirToPlayer = (toPosition.position - plane.position).normalized;

                // Create a rotation that points the object's Y-axis (top) at the target
                Quaternion planeRotation = Quaternion.FromToRotation(Vector3.up, dirToPlayer);

                plane.rotation = planeRotation;
            }

            /*
            if (m_light)
            {
                // Calculate the direction from this object to the target
                dirToPlayer = (m_light.transform.position - m_toPosition.transform.position).normalized;

                // Create a rotation that points the object's negative Y-axis (bottom) at the target
                Quaternion lightRotation = Quaternion.FromToRotation(Vector3.up, dirToPlayer);

                m_light.transform.rotation = lightRotation;
            }
            */
        }

        public static void TryLook(Transform horizonPlaneRoot, Transform spaceCamRoot, EqCoords coords)
        {

            SpaceCameraState state = Find.State<SpaceCameraState>();
            var dome = Find.State<SkyDome>();
            float skyboxDist = dome.Radius;

            float raDegrees = (float)CoordinateUtility.RAToDegrees((int)coords.RightAscension.Hours, (int)coords.RightAscension.Minutes, coords.RightAscension.Seconds);
            float declDegrees = (float)CoordinateUtility.DeclinationToDecimalDegrees((int)coords.Declination.Hours, (int)coords.Declination.Minutes, coords.Declination.Seconds);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees) * skyboxDist;

            var lookPos = dome.Position + posOffset * skyboxDist;

            spaceCamRoot.LookAt(lookPos, horizonPlaneRoot.up);
            var angles = spaceCamRoot.transform.localEulerAngles;
            angles.z = 0;
            spaceCamRoot.transform.localEulerAngles = angles;

            state.HorizLook = angles.y;
            state.VertLook = angles.x > 90 ? angles.x - 360 : angles.x;
        }
    }
}
