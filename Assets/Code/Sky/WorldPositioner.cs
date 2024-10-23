using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class WorldPositioner : MonoBehaviour
    {
        public static WorldPositioner Instance;

        [SerializeField] private GameObject m_toPosition;
        [SerializeField] private GameObject m_relativeTo;
        [SerializeField] private GameObject m_plane;
        [SerializeField] private GameObject m_light;

        [SerializeField] private float m_heightOffset;

        [Header("Radians")]
        [SerializeField] private Vector3 m_lat;
        [SerializeField] private Vector3 m_long;

        [Header("Rasc-Decl")]
        [SerializeField] private HmsCoords m_rightAscension;
        [SerializeField] private HmsCoords m_declination;

        [Header("Camera")]
        [SerializeField] private Transform m_camRoot;
        [SerializeField] private Transform m_cam;

        private void Awake()
        {
            if (Instance == null) { Instance = this; }
            else if (Instance != this) { Destroy(this.gameObject); return; }
        }

        public void PositionAtLatLongVector3(Vector3 lat, Vector3 longitude)
        {
            float latDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
                (int)lat.x,
                (int)lat.y,
                lat.z);

            float longDegrees = (float)CoordinateUtility.DegreesToDecimalDegrees(
                (int)longitude.x,
                (int)longitude.y,
                longitude.z);

            PositionAtLatLongDegrees(latDegrees, longDegrees);
        }

        public void PositionAtLatLongDegrees(float latDegrees, float longDegrees)
        {
            var pos = CoordinateUtility.LatLongToCartesianCoordinates(latDegrees, longDegrees);

            pos *= (m_relativeTo.transform.localScale.x / 2);
            pos.y += m_heightOffset * Mathf.Sign(pos.y);

            m_toPosition.transform.position = pos;

            // Calculate the direction from this object to the target
            Vector3 directionToTarget = (m_relativeTo.transform.position - m_toPosition.transform.position).normalized;

            // Create a rotation that points the object's negative Y-axis (bottom) at the target
            Quaternion targetRotation = Quaternion.FromToRotation(Vector3.down, directionToTarget);

            // Apply the rotation to the object
            m_toPosition.transform.rotation = targetRotation;

            Vector3 dirToPlayer;

            if (m_plane)
            {
                // Calculate the direction from this object to the target
                dirToPlayer = (m_toPosition.transform.position - m_plane.transform.position).normalized;

                // Create a rotation that points the object's Y-axis (top) at the target
                Quaternion planeRotation = Quaternion.FromToRotation(Vector3.up, dirToPlayer);

                m_plane.transform.rotation = planeRotation;
            }

            if (m_light)
            {
                /*
                // Calculate the direction from this object to the target
                dirToPlayer = (m_light.transform.position - m_toPosition.transform.position).normalized;

                // Create a rotation that points the object's negative Y-axis (bottom) at the target
                Quaternion lightRotation = Quaternion.FromToRotation(Vector3.up, dirToPlayer);

                m_light.transform.rotation = lightRotation;
                */
            }
        }

#if UNITY_EDITOR
        
        [ContextMenu("(Degrees) Set Position at Lat-Long")]
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

        [ContextMenu("Look at Rasc Decl")]
        private void LookRascDecl()
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
#endif // UNITY_EDITOR

    }
}