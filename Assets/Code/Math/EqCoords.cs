using BeauUtil;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Astro {
    [Serializable]
    public struct EqCoords : IEquatable<EqCoords> {
        public HmsCoords RightAscension;
        public HmsCoords Declination;

        public bool Equals(EqCoords other) {
            return RightAscension.Equals(other.RightAscension)
                && Declination.Equals(other.Declination);
        }
    }
}