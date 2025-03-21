using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Astro
{
    public static class CoordinateUtility
    {
        public const float Hour2Deg = 15f;
        public const float Deg2Hour = 1f / Hour2Deg;

        #region Angles

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float DegToRad(float degree) {
            return degree * Mathf.Deg2Rad;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float RadToDeg(float radian) {
            return radian * Mathf.Rad2Deg;
        }

        #endregion // Angles

        #region Right Ascension

        /// <summary>
        /// Converts Right Ascension hours, minutes, and seconds into degrees
        /// </summary>
        public static float HmsToDD(HmsCoords ra) {
            return HmsToDD(ra.Hours, ra.Minutes, ra.Seconds);
        }

        /// <summary>
        /// Converts Right Ascension hours, minutes, and seconds into degrees
        /// </summary>
        public static float HmsToDD(int hours, int minutes, float seconds) {
            // Convert hours, minutes, and seconds to degrees
            return Hour2Deg * (hours + (minutes / 60f) + (seconds / 3600f));
        }

        /// <summary>
        /// Converts degrees to Right Ascension.
        /// </summary>
        public static HmsCoords DDToHms(float degrees) {
            int hrs, minutes = 0;
            float seconds = 0;

            degrees *= Deg2Hour;
            hrs = (int) degrees;
            degrees = (degrees - hrs) * 60;

            minutes = (int) degrees;
            degrees = (degrees - minutes) * 60;

            seconds = degrees;

            return new HmsCoords(hrs, minutes, seconds);
        }

        public static DmsCoords HmsToDms(HmsCoords ra) {
            float deg = HmsToDD(ra);
            return DDToDms(deg);
        }

        #endregion // Right Ascension

        #region Declination

        /// <summary>
        /// Converts declination degrees, minutes, and seconds into degrees
        /// </summary>
        public static float DmsToDD(DmsCoords declination) {
            return DmsToDD(declination.Degrees, declination.Minutes, declination.Seconds);
        }

        /// <summary>
        /// Converts declination degrees, minutes, and seconds into degrees
        /// </summary>
        public static float DmsToDD(int deg, int minutes, float seconds) {
            // Convert hours, minutes, and seconds to degrees
            return (deg + (minutes / 60f) + (seconds / 3600f));
        }

        /// <summary>
        /// Converts decimal degrees into declination.
        /// </summary>
        public static DmsCoords DDToDms(float degrees) {
            int deg, minutes = 0;
            float seconds = 0;

            deg = (int) degrees;
            degrees = (degrees - deg) * 60;

            minutes = (int) degrees;
            degrees = (degrees - minutes) * 60;

            seconds = degrees;

            return new DmsCoords(deg, minutes, seconds);
        }

        public static HmsCoords DmsToHms(DmsCoords declination) {
            float deg = DmsToDD(declination);
            return DDToHms(deg);
        }

        #endregion // Declination

        #region Lat/Long

        /// <summary>
        /// Retrieves the rotation represented by the given latitude and longitude.
        /// </summary>
        public static Quaternion LatLongRotation(DmsCoords lat, DmsCoords longitude) {
            float latDegrees = DmsToDD(lat);
            float longDegrees = DmsToDD(longitude);

            return LatLongRotation(latDegrees, longDegrees);
        }

        public static Quaternion LatLongRotation(float latDegrees, float longDegrees) {
            return Quaternion.LookRotation(LatLongToCartesianCoordinates(latDegrees, longDegrees), Vector3.up);
        }

        #endregion // Lat/Long

        #region Cartesian Coordinates

        /// <summary>
        /// Converts right ascension and declination into cartesian coordinates on a unit
        /// sphere (r = 1)
        /// </summary>
        public static Vector3 LatLongRadiansToCartesianCoordinates(float latRad, float longRad) {
            // Note: Skybox map is rotated -90 degrees in the z from expected calculations

            // Expected
            float x = (float) (Math.Cos(latRad) * Math.Cos(longRad));
            float y = (float) (Math.Cos(latRad) * Math.Sin(longRad));
            float z = (float) Math.Sin(latRad);


            // Corrected
            float correctedX = x;
            float correctedY = y;
            float correctedZ = z;

            return new Vector3((float) correctedX, (float) correctedY, (float) correctedZ);
        }

        /// <summary>
        /// Converts right ascension and declination into cartesian coordinates on a unit
        /// sphere (r = 1)
        /// </summary>
        /// <param name="rAsc">in degrees</param>
        /// <param name="decl"></param>
        public static Vector3 LatLongToCartesianCoordinates(float lat, float longitude) {
            float latRad = DegToRad(lat);
            float longRad = DegToRad(longitude);

            return LatLongRadiansToCartesianCoordinates((float) latRad, (float) longRad);
        }

        /// <summary>
        /// Converts right ascension and declination into cartesian coordinates on a unit
        /// sphere (r = 1)
        /// </summary>
        /// <param name="rAsc">in degrees</param>
        /// <param name="decl"></param>
        public static Vector3 RAscDeclDegreesToCartesianCoordinates(float rAsc, float decl) {
            float raRad = DegToRad(rAsc);
            float decRad = DegToRad(decl);

            return RAscDeclRadiansToCartesianCoordinates((float) raRad, (float) decRad);
        }

        public static Vector3 RAscDeclRadiansToCartesianCoordinates(float raRad, float decRad) {
            // Note: Skybox map is rotated -90 degrees in the z from expected calculations

            // Expected
            float x = (float) (Math.Cos(raRad) * Math.Cos(decRad));
            float y = (float) (Math.Sin(raRad) * Math.Cos(decRad));
            float z = (float) (Math.Sin(decRad));


            // Corrected
            float correctedX = x;
            float correctedY = y;
            float correctedZ = z;

            correctedX = -y;
            correctedY = z;
            correctedZ = x;

            return new Vector3((float) correctedX, (float) correctedY, (float) correctedZ);
        }

        #endregion // Cartesian Coordinates

        //public static Vector2 CartesianToPolar(Vector3 cartPoint)
        //{
        //    Vector2 polarPoint;

        //    //calc longitude
        //    polarPoint.y = Mathf.Atan2(cartPoint.x, cartPoint.z);

        //    //this is easier to write and read than sqrt(pow(x,2), pow(y,2))!
        //    var xzLen = new Vector2(cartPoint.x, cartPoint.z).magnitude;

        //    //atan2 does the magic
        //    polarPoint.x = Mathf.Atan2(-cartPoint.y,xzLen);

        //    //convert to deg
        //    polarPoint *= Mathf.Rad2Deg;

        //    return polarPoint;
        //}


        //public static Vector3 PolarToCartesian(Vector2 polarPoint)
        //{

        //    //an origin vector, representing lat,lon of 0,0. 
        //    var origin = new Vector3(0, 0, 1);
             
        //    // build a quaternion using euler angles for lat,lon
        //    var rotation = Quaternion.Euler(polarPoint.x, polarPoint.y, 0);

        //    //transform our reference vector by the rotation. Easy-peasy!
        //    Vector3 cartPoint = rotation * origin;

        //    return cartPoint;
        //}

        //public static Vector3 RAscDeclToSphericalCoordinates(float rAsc, float decl)
        //{
        //    double raRad = Math.PI * rAsc / 180.0;
        //    double decRad = Math.PI * decl / 180.0;

        //    double x = Math.Cos(raRad) * Math.Cos(decRad);
        //    double y = Math.Sin(raRad) * Math.Cos(decRad);
        //    double z = Math.Sin(decRad);

        //    double r = Math.Sqrt(x * x + y * y + z * z);
        //    double inclination = Math.Acos(z / r) * 180.0 / Math.PI;
        //    double azimuth = Math.Atan2(y, x) * 180.0 / Math.PI;

        //    return new Vector3((float)r, (float)inclination, (float)azimuth);
        //}

        //public static Vector3 SphericalToCartesianCoordinates(float radius, float inclination, float azimuth)
        //{
        //    double inclinationRad = Math.PI * inclination / 180.0;
        //    double azimuthRad = Math.PI * azimuth / 180.0;

        //    double x = radius * Math.Sin(inclinationRad) * Math.Cos(azimuthRad);
        //    double y = radius * Math.Sin(inclinationRad) * Math.Sin(azimuthRad);
        //    double z = radius * Math.Cos(inclinationRad);

        //    return new Vector3((float)x, (float)y, (float)z);
        //}
    }

}