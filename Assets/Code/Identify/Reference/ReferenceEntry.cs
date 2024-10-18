using FieldDay.Assets;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/Entry")]
    public sealed class ReferenceEntry : NamedAsset {

    }

    public sealed class ReferenceEntryId : AssetNameAttribute {
        public ReferenceEntryId() : base(typeof(ReferenceEntry)) { }
    }
}