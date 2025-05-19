using BeauUtil;
using FieldDay.Assets;
using System;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/Classification")]
    public sealed class ReferenceClassification : NamedAsset {
        public string Label;
        public ClassificationTypeMask Type;
        public ReferenceDetail[] Details;
    }

    public sealed class ClassificationIdAttribute : AssetNameAttribute {
        public ClassificationIdAttribute() : base(typeof(ReferenceClassification)) { }
    }

    [Flags]
    public enum ClassificationTypeMask {
        Photometer = 0x01,
        ColorMeter = 0x02,
        Spectrometer = 0x04,
        Historical = 0x08,
        Infrared = 0x10,

        [Hidden] ALL = Photometer | ColorMeter | Spectrometer | Historical | Infrared
    }
}