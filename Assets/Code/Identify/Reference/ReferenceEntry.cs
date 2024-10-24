using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/Entry")]
    public sealed class ReferenceEntry : NamedAsset {

    }

    public sealed class ReferenceEntryIdAttribute : AssetNameAttribute {
        public ReferenceEntryIdAttribute() : base(typeof(ReferenceEntry)) { }
    }
}