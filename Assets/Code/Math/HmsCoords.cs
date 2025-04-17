using BeauUtil;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct HmsCoords : IEquatable<HmsCoords> {
        public short Hours;
        public short Minutes;
        public float Seconds;

        public HmsCoords(Vector3 vec) {
            Hours = (short) vec.x;
            Minutes = (short) vec.y;
            Seconds = vec.z;

            CoordinateUtility.SanitizeHms(ref Hours, ref Minutes, ref Seconds);
        }

        public HmsCoords(int hours, int minutes, float seconds) {
            Hours = (short) hours;
            Minutes = (short) minutes;
            Seconds = seconds;

            CoordinateUtility.SanitizeHms(ref Hours, ref Minutes, ref Seconds);
        }

        public void Sanitize() {
            CoordinateUtility.SanitizeHms(ref Hours, ref Minutes, ref Seconds);
        }

        #region Overrides

        public bool Equals(HmsCoords coords) {
            return Hours == coords.Hours
                && Minutes == coords.Minutes
                && Seconds == coords.Seconds;
        }

        public override bool Equals(object obj) {
            if (obj is HmsCoords) {
                return Equals((HmsCoords)obj);
            }
            return false;
        }

        public override int GetHashCode() {
            int hash = Hours.GetHashCode();
            hash = (hash << 5) ^ Minutes.GetHashCode();
            hash = (hash >> 3) ^ Seconds.GetHashCode();
            return hash;
        }

        #endregion // Overrides

        #region ToString

        public override string ToString() {
            return string.Format("{0}h {1}m {2:F1}s", Hours.ToStringLookup(), Minutes.ToStringLookup(), Seconds);
        }

        public void ToString(StringBuilder sb) {
            sb.AppendNoAlloc(Hours).Append("h ")
                .AppendNoAlloc(Minutes, 0, 2).Append("m ")
                .AppendNoAlloc(Seconds, 1, 2).Append('s');
        }

        #endregion // ToString

        #region Operators

        static public HmsCoords operator+(HmsCoords a, HmsCoords b) {
            int h = a.Hours + b.Hours;
            int m = a.Minutes + b.Minutes;
            float s = a.Seconds + b.Seconds;

            CoordinateUtility.SanitizeHms(ref h, ref m, ref s);
            return new HmsCoords(h, m, s);
        }

        static public HmsCoords operator -(HmsCoords a, HmsCoords b) {
            int h = a.Hours - b.Hours;
            int m = a.Minutes - b.Minutes;
            float s = a.Seconds - b.Seconds;

            CoordinateUtility.SanitizeHms(ref h, ref m, ref s);
            return new HmsCoords(h, m, s);
        }

        static public HmsCoords operator-(HmsCoords a) {
            return new HmsCoords(-a.Hours, -a.Minutes, -a.Seconds);
        }

        static public bool operator==(HmsCoords a, HmsCoords b) {
            return a.Equals(b);
        }

        static public bool operator !=(HmsCoords a, HmsCoords b) {
            return !a.Equals(b);
        }

        #endregion // Operators
    }
}