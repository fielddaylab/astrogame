using System;

namespace Astro {
    [Serializable]
    public struct LatLongDecCoords : IEquatable<LatLongDecCoords> {
        public float Latitude;
        public float Longitude;

        public LatLongDecCoords(float latitude, float longitude) {
            Latitude = latitude;
            Longitude = longitude;
        }

        public bool Equals(LatLongDecCoords other) {
            return Latitude == other.Latitude && Longitude == other.Longitude;
        }
    }

    [Serializable]
    public struct LatLongCoords : IEquatable<LatLongCoords> {
        public DmsCoords Latitude;
        public DmsCoords Longitude;

        public LatLongCoords(DmsCoords latitude, DmsCoords longitude) {
            Latitude = latitude;
            Longitude = longitude;
        }

        public bool Equals(LatLongCoords other) {
            return Latitude == other.Latitude && Longitude == other.Longitude;
        }
    }
}