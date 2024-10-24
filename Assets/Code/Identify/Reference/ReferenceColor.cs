using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/Color")]
    public sealed class ReferenceColor : NamedAsset {
        public string Label;
        public uint MinTemperature;
        public uint MaxTemperature;
    }

    public sealed class ColorIdAttribute : AssetNameAttribute {
        public ColorIdAttribute() : base(typeof(ReferenceColor)) { }
    }
}