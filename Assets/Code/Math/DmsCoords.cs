using BeauUtil;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;

namespace Astro {
    [Serializable]
    public struct DmsCoords : IEquatable<DmsCoords> {
        [FormerlySerializedAs("Hours")] public short Degrees;
        public short Minutes;
        public float Seconds;

        public DmsCoords(Vector3 vec) {
            Degrees = (short) vec.x;
            Minutes = (short) vec.y;
            Seconds = vec.z;

            CoordinateUtility.Sanitize(ref Degrees, ref Minutes, ref Seconds);
        }

        public DmsCoords(int degrees, int minutes, float seconds) {
            Degrees = (short) degrees;
            Minutes = (short) minutes;
            Seconds = seconds;

            CoordinateUtility.Sanitize(ref Degrees, ref Minutes, ref Seconds);
        }

        public void Sanitize() {
            CoordinateUtility.Sanitize(ref Degrees, ref Minutes, ref Seconds);
        }

        #region Overrides

        public bool Equals(DmsCoords coords) {
            return Degrees == coords.Degrees
                && Minutes == coords.Minutes
                && Seconds == coords.Seconds;
        }

        public override bool Equals(object obj) {
            if (obj is DmsCoords) {
                return Equals((DmsCoords) obj);
            }
            return false;
        }

        public override int GetHashCode() {
            int hash = Degrees.GetHashCode();
            hash = (hash << 5) ^ Minutes.GetHashCode();
            hash = (hash >> 3) ^ Seconds.GetHashCode();
            return hash;
        }

        #endregion // Overrides

        #region ToString

        public override string ToString() {
            return string.Format("{0}{1}\u00B0 {2}' {3:F1}\"", Degrees >= 0 ? "+" : "", Degrees.ToStringLookup(), Minutes.ToStringLookup(), Seconds);
        }

        public void ToString(StringBuilder sb) {
            if (Degrees >= 0) {
                sb.Append('+');
            }
            sb.AppendNoAlloc(Degrees).Append("\u00B0 ")
                .AppendNoAlloc(Minutes, 0, 2).Append("' ")
                .AppendNoAlloc(Seconds, 1, 2).Append('"');
        }

        #endregion // ToString

        #region Operators

        static public DmsCoords operator +(DmsCoords a, DmsCoords b) {
            int h = a.Degrees + b.Degrees;
            int m = a.Minutes + b.Minutes;
            float s = a.Seconds + b.Seconds;

            CoordinateUtility.Sanitize(ref h, ref m, ref s);
            return new DmsCoords(h, m, s);
        }

        static public DmsCoords operator -(DmsCoords a, DmsCoords b) {
            int h = a.Degrees - b.Degrees;
            int m = a.Minutes - b.Minutes;
            float s = a.Seconds - b.Seconds;

            CoordinateUtility.Sanitize(ref h, ref m, ref s);
            return new DmsCoords(h, m, s);
        }

        static public DmsCoords operator -(DmsCoords a) {
            return new DmsCoords(-a.Degrees, -a.Minutes, -a.Seconds);
        }

        static public bool operator ==(DmsCoords a, DmsCoords b) {
            return a.Equals(b);
        }

        static public bool operator !=(DmsCoords a, DmsCoords b) {
            return !a.Equals(b);
        }

        #endregion // Operators
    }
}