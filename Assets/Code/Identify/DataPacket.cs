using System;
using System.Runtime.InteropServices;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay.Assets;

namespace Astro {
    /// <summary>
    /// Packet of data.
    /// </summary>
    public struct DataPacket : IEquatable<DataPacket> {
        public readonly DataTypeMask Type;
        public readonly StringHash32 HistoricalPatternId;
        public readonly Datum Value;

        #region Constructors

        private DataPacket(DataTypeMask type, StringHash32 patternId, Datum value) {
            Assert.True((type & (type - 1)) == 0, "Cannot specify combined mask '{0}' as type for data packet", type);
            Assert.True(type >= DataTypeMask.Historical_Coordinates, "Cannot specify pattern for non-historical data type '{0}'", type);
            Type = type;
            HistoricalPatternId = patternId;
            Value = value;
        }

        private DataPacket(DataTypeMask type, Datum value) {
            Assert.True((type & (type - 1)) == 0, "Cannot specify combined mask '{0}' as type for data packet", type);
            Assert.True(type < DataTypeMask.Historical_Coordinates, "Pattern required for historical data type '{0}'", type);
            Type = type;
            HistoricalPatternId = default;
            Value = value;
        }

        #endregion // Constructors

        #region Factory

        static public DataPacket Name(CelestialAsset asset) {
            return new DataPacket(DataTypeMask.Name, new Datum() {
                AssetId = asset.AssetId
            });
        }

        static public DataPacket Coordinates(EqCoords coordinates) {
            return new DataPacket(DataTypeMask.Coordinates, new Datum() {
                Coordinates = coordinates
            });
        }

        static public DataPacket HistoricalCoordinates(EqCoords coordinates, HistoricalPatternAsset pattern) {
            return new DataPacket(DataTypeMask.Historical_Coordinates, AssetUtility.IdOf(pattern), new Datum() {
                Coordinates = coordinates
            });
        }

        static public DataPacket Color(ReferenceColor colorRef) {
            return new DataPacket(DataTypeMask.Color, new Datum() {
                AssetId = colorRef.AssetId
            });
        }

        static public DataPacket HistoricalColor(ReferenceColor colorRef, HistoricalPatternAsset pattern) {
            return new DataPacket(DataTypeMask.Historical_Color, AssetUtility.IdOf(pattern), new Datum() {
                AssetId = colorRef.AssetId
            });
        }

        static public DataPacket ApparentMagnitude(double magnitude) {
            return new DataPacket(DataTypeMask.ApparentMagnitude, new Datum() {
                Magnitude = magnitude
            });
        }

        static public DataPacket HistoricalApparentMagnitude(double magnitude, HistoricalPatternAsset pattern) {
            return new DataPacket(DataTypeMask.Historical_ApparentMagnitude, AssetUtility.IdOf(pattern), new Datum() {
                Magnitude = magnitude
            });
        }

        static public DataPacket AbsoluteMagnitude(double magnitude) {
            return new DataPacket(DataTypeMask.AbsoluteMagnitude, new Datum() {
                Magnitude = magnitude
            });
        }

        static public DataPacket Spectrograph(SpectrographMaterialMask materials) {
            return new DataPacket(DataTypeMask.MaterialSpectrum, new Datum() {
                Materials = materials
            });
        }

        static public DataPacket Temperature(double temperature) {
            return new DataPacket(DataTypeMask.Temperature, new Datum() {
                Temperature = temperature
            });
        }

        static public DataPacket HistoricalTemperature(double temperature, HistoricalPatternAsset pattern) {
            return new DataPacket(DataTypeMask.Historical_Temperature, AssetUtility.IdOf(pattern), new Datum() {
                Temperature = temperature
            });
        }

        static public DataPacket Distance(double distance) {
            return new DataPacket(DataTypeMask.Distance, new Datum() {
                Distance = distance
            });
        }

        static public DataPacket HistoricalDistance(double distance, HistoricalPatternAsset pattern) {
            return new DataPacket(DataTypeMask.Historical_Distance, AssetUtility.IdOf(pattern), new Datum() {
                Distance = distance
            });
        }

        #endregion // Factory
    
        public bool Equals(DataPacket other) {
            return Type == other.Type
                && HistoricalPatternId == other.HistoricalPatternId
                && Value.Equals(other.Value);
        }
    }

    /// <summary>
    /// Union of data types. Should be 16 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct Datum : IEquatable<Datum> {
        [FieldOffset(0)] public StringHash32 AssetId;
        [FieldOffset(0)] public EqCoords Coordinates;
        [FieldOffset(0)] public double Magnitude;
        [FieldOffset(0)] public SpectrographMaterialMask Materials;
        [FieldOffset(0)] public double Temperature;
        [FieldOffset(0)] public double Distance;

        [FieldOffset(0)] private ulong Raw0;
        [FieldOffset(8)] private ulong Raw1;

        public bool Equals(Datum other) {
            return Raw0 == other.Raw0
                && Raw1 == other.Raw1;
        }
    }
}