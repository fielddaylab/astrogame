using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Celestial Asset")]
    public sealed class CelestialAsset : NamedAsset {
        #region Inspector

        public string DisplayName;
        public EqCoords Coords;

        [Header("Categorization")]
        public CelestialObjectCategory Category;
        [ClassificationId] public StringHash32[] ClassIds;
        [ReferenceEntryId] public StringHash32 ReferenceId;
        [ConstellationId] public StringHash32 ConstellationId;

        [Header("Temperature and Color")]
        public uint Temperature;
        [ColorId] public StringHash32 ColorId;

        [Header("Magnitude")]
        public float ApparentMagnitude;
        public float AbsoluteMagnitude;

        [Header("Materials")]
        public SpectrographMaterialMask Spectrograph;

        [Header("Constellation Path")]
        [AssetName(typeof(SkyRegionBounds))] public StringHash32 ConstellationBoundaryId;

        #endregion // Inspector
    }

    public enum CelestialObjectCategory {
        Planet,
        Star,
        Comet,
        Satellite,
        Constellation,
        Galaxy
    }

    public sealed class ConstellationIdAttribute : AssetNameAttribute {
        public ConstellationIdAttribute() : base(typeof(CelestialAsset), true) { }

        protected override bool Predicate(UnityEngine.Object obj) {
            return ((CelestialAsset) obj).Category == CelestialObjectCategory.Constellation;
        }
    }
}
