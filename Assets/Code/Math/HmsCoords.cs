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
        }

        public HmsCoords(int hours, int minutes, float seconds) {
            Hours = (short) hours;
            Minutes = (short) minutes;
            Seconds = seconds;
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
            return ToString(HmsPrefix.Hms);
        }

        public string ToString(HmsPrefix prefix) {
            if (prefix == HmsPrefix.Hms) {
                return string.Format("{0}h {1}m {2:F4}s", Hours.ToStringLookup(), Minutes.ToStringLookup(), Seconds);
            }

            return string.Format("{0}{1}\u00B0 {2}' {3:F4}\"", Hours >= 0 ? "+" : "", Hours.ToStringLookup(), Minutes.ToStringLookup(), Seconds);
        }

        public void ToString(StringBuilder sb, HmsPrefix prefix) {
            if (prefix == HmsPrefix.Hms) {
                sb.AppendNoAlloc(Hours).Append("h ")
                    .AppendNoAlloc(Minutes).Append("m ")
                    .AppendNoAlloc(Seconds, 4).Append('s');
            } else {
                if (Hours >= 0) {
                    sb.Append('+');
                }
                sb.AppendNoAlloc(Hours).Append("\u00B0 ")
                    .AppendNoAlloc(Minutes).Append("' ")
                    .AppendNoAlloc(Seconds, 4).Append('"');
            }
        }

        #endregion // ToString

        #region Operators

        static public bool operator==(HmsCoords a, HmsCoords b) {
            return a.Equals(b);
        }

        static public bool operator !=(HmsCoords a, HmsCoords b) {
            return !a.Equals(b);
        }

        #endregion // Operators
    }

    public enum HmsPrefix : byte {
        Hms,
        Quotes
    }
}