using Astro;
using FieldDay.Systems;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Astro
{
    public class WorldPositionSystem : SystemBehaviour
    {
        
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

        /*
        public void MenuPositionAtLatLong()
        {
            float latDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
                (int)m_lat.x,
                (int)m_lat.y,
                m_lat.z);

            float longDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
                (int)m_long.x,
                (int)m_long.y,
                m_long.z);

            PositionAtLatLongDegrees(latDegrees, longDegrees);
        }

        private void MenuLookRascDecl()
        {
            int skyboxDist = 1000;

            float raDegrees = (float)CoordinateUtility.RAToDegrees((int)m_rightAscension.Hours, (int)m_rightAscension.Minutes, m_rightAscension.Seconds);
            float declDegrees = (float)CoordinateUtility.DeclensionToDecimalDegrees((int)m_declination.Hours, (int)m_declination.Minutes, m_declination.Seconds);
            var pos = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees) * skyboxDist;

            m_camRoot.LookAt(pos, m_toPosition.transform.up);
            var angles = m_camRoot.transform.localEulerAngles;
            angles.x = 0;
            m_camRoot.transform.localEulerAngles = angles;
        }
        */
    }
}
