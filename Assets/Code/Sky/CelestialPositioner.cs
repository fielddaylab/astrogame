using FieldDay.Components;
using UnityEngine;

namespace Astro {
    public sealed class CelestialPositioner : BatchedComponent {

    }

    public static class CelestialPositionerUtility
    {
        /// <summary>
        /// Position an object given right ascension and declination in hours, minutes, and seconds
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="toPosition"></param>
        /// <param name="ra"></param>
        /// <param name="decl"></param>
        public static void PositionObject(Vector3 centerPos, GameObject toPosition, HmsCoords ra, HmsCoords decl)
        {
            // TODO: store skybox dist as a const
            int skyboxDist = 1000; // FindObjectOfType<GameConsts>().SkyboxDist;

            float raDegrees = (float)CoordinateUtility.RAToDegrees((int)ra.Hours, (int)ra.Minutes, ra.Seconds);
            float declDegrees = (float)CoordinateUtility.DeclensionToDecimalDegrees((int)decl.Hours, (int)decl.Minutes, decl.Seconds);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);
            toPosition.transform.position = centerPos + posOffset * skyboxDist;
        }

        /// <summary>
        /// Position an object given right ascension and declination in radians
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="toPosition"></param>
        /// <param name="ra"></param>
        /// <param name="decl"></param>
        public static void PositionObject(Vector3 centerPos, GameObject toPosition, float raRad, float declRad)
        {
            // TODO: store skybox dist as a const
            int skyboxDist = 1000; // FindObjectOfType<GameConsts>().SkyboxDist;

            var posOffset = CoordinateUtility.RAscDeclRadiansToCartesianCoordinates(raRad, declRad);
            toPosition.transform.position = centerPos + posOffset * skyboxDist;
        }
    }
}