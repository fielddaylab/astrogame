using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/Classification")]
    public sealed class ReferenceClassification : NamedAsset {
        public string Label;
    }

    public sealed class ClassificationIdAttribute : AssetNameAttribute {
        public ClassificationIdAttribute() : base(typeof(ReferenceClassification)) { }
    }
}