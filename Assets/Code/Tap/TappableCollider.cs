using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Collider))]
    public sealed class TappableCollider : BatchedComponent {
        [AssetName(typeof(TappableMaterial), true)] public StringHash32 Material;
    }
}