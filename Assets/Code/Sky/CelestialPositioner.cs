using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    public static class CelestialPositionerUtility
    {
        /// <summary>
        /// Position an object given right ascension and declination in hours, minutes, and seconds
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="ra"></param>
        /// <param name="decl"></param>
        public static Vector3 GetObjectPosition(Vector3 centerPos, HmsCoords ra, DmsCoords decl)
        {
            SkyDome dome = Find.State<SkyDome>();
            float skyboxDist = dome.Radius;

            float raDegrees = (float)CoordinateUtility.HmsToDD(ra);
            float declDegrees = (float)CoordinateUtility.DmsToDD(decl);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);

            return centerPos + posOffset * skyboxDist;
        }

        /// <summary>
        /// Position an object given right ascension and declination in hours, minutes, and seconds
        /// </summary>
        /// <param name="centerPos"></param>
        /// <param name="toPosition"></param>
        /// <param name="ra"></param>
        /// <param name="decl"></param>
        public static void PositionObject(Vector3 centerPos, Transform toPosition, HmsCoords ra, DmsCoords decl) {
            SkyDome dome = Find.State<SkyDome>();
            float skyboxDist = dome.Radius;

            float raDegrees = (float)CoordinateUtility.HmsToDD(ra);
            float declDegrees = (float)CoordinateUtility.DmsToDD(decl);
            var posOffset = CoordinateUtility.RAscDeclDegreesToCartesianCoordinates(raDegrees, declDegrees);
            toPosition.position = centerPos + posOffset * skyboxDist;
        }
    }
}