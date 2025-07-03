using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Assets;
using System;
using System.Collections;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Celestial Asset")]
    public sealed class CelestialAsset : NamedAsset {
        #region Inspector

        public string DisplayName;
        public EqCoords Coords;
        public CelestialObjectVisMask Visibility = CelestialObjectVisMask.Visible;

        [Header("Categorization")]
        public CelestialObjectCategory Category;
        [ClassificationId] public StringHash32[] ClassIds; 
        [ReferenceEntryId] public StringHash32 ReferenceId; // deprecated
        [ConstellationId] public StringHash32 ConstellationId; // deprecated

        public bool AtypicalForClass;

        [Header("Temperature and Color")]
        public uint Temperature;
        [ColorId] public StringHash32 ColorId; // deprecated

        [Header("Magnitude")]
        public float ApparentMagnitude; // visible
        public float ApparentBlueMagnitude;
        public float ApparentIRMagnitude;
        public float AbsoluteMagnitude; // visible 
        public HistoricalPatternAsset HistoricalBrightness; // deprecated

        [Header("Materials")]
        public SpectrographMaterialMask Spectrograph;

        [Header("Distance")]
        public float Distance;

        [Header("Constellation Path")]
        [AssetName(typeof(SkyRegionBounds))] public StringHash32 ConstellationBoundaryId;

#if UNITY_EDITOR
        private void OnValidate() {
            if ((Visibility & CelestialObjectVisMask.Visible) != 0) {
                Visibility |= CelestialObjectVisMask.Blue | CelestialObjectVisMask.Infrared;
            }
        }
#endif // UNITY_EDITOR

        #endregion // Inspector

        /// <summary>
        /// Creates a DataPacket given a SINGLE data type flag and CelestialAsset.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        static public DataPacket MaskAssetToData(DataTypeMask type, CelestialAsset asset) {
            Assert.True((type & (type - 1)) == 0, "Cannot specify combined mask '{0}' as type for data packet", type);
            
            switch (type) {
                case DataTypeMask.Name: {
                        return DataPacket.Name(asset);
                    }
                case DataTypeMask.Coordinates: {
                        return DataPacket.Coordinates(asset.Coords);
                    }
                case DataTypeMask.Color: {
                        return DataPacket.Color(asset.ColorId);
                    }
                case DataTypeMask.ColorIndex: {
                    return DataPacket.ColorIndex(asset.ApparentBlueMagnitude - asset.ApparentMagnitude);
                }
                case DataTypeMask.ApparentMagnitude: {
                        return DataPacket.ApparentMagnitude(asset.ApparentMagnitude);
                    }
                case DataTypeMask.AbsoluteMagnitude: {
                        return DataPacket.AbsoluteMagnitude(asset.AbsoluteMagnitude);
                    }
                case DataTypeMask.MaterialSpectrum: {
                        return DataPacket.Spectrograph(asset.Spectrograph);
                    }
                case DataTypeMask.Temperature: {
                        return DataPacket.Temperature(asset.Temperature);
                    }
                case DataTypeMask.Distance: {
                        return DataPacket.Distance(asset.Distance);
                    }
                case DataTypeMask.Historical_ApparentMagnitude: {
                        return DataPacket.HistoricalApparentMagnitude(asset.HistoricalBrightness);
                    }
                case DataTypeMask.BlueMagnitude: {
                        return DataPacket.BlueMagnitude(asset.ApparentBlueMagnitude);
                    }
                case DataTypeMask.InfraredMagnitude: {
                        return DataPacket.InfraredMagnitude(asset.ApparentIRMagnitude);
                    }
                case DataTypeMask.Historical_Coordinates:
                case DataTypeMask.Historical_Color: {
                        Log.Error("[CelestialAsset.MaskAssetToData] celestial asset historical data unimplemented!");
                        throw new ArgumentException("DataMaskType " + type + " not implemented");                    }
                default: {
                        throw new ArgumentException("DataMaskType "+type+" not implemented");
                    }
                    
            }
        }
    }

    public enum CelestialObjectCategory {
        Planet,
        Star,
        Comet,
        Satellite,
        Constellation,
        Galaxy
    }

    [Flags]
    public enum CelestialObjectVisMask {
        Visible = 0x01,
        Infrared = 0x02,
        Blue = 0x04,
    }
    public sealed class ConstellationIdAttribute : AssetNameAttribute {
        public ConstellationIdAttribute() : base(typeof(CelestialAsset), true) { }

        protected override bool Predicate(UnityEngine.Object obj) {
            return ((CelestialAsset) obj).Category == CelestialObjectCategory.Constellation;
        }
    }
}
