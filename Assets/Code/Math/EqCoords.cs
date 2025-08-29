using BeauUtil;
using FieldDay;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct EqCoords : IEquatable<EqCoords> {
        public HmsCoords RightAscension;
        public DmsCoords Declination;

        public EqCoords(HmsCoords ra, DmsCoords dec) {
            RightAscension = ra;
            Declination = dec;
        }

        public bool Equals(EqCoords other) {
            return RightAscension.Equals(other.RightAscension)
                && Declination.Equals(other.Declination);
        }

        public readonly JsonBuilder Append(JsonBuilder json)
        {
            json.BeginObject("right_ascension");
            RightAscension.Append(json).EndObject();
            json.BeginObject("declination");
            Declination.Append(json).EndObject();
            return json;
        }
    }
}