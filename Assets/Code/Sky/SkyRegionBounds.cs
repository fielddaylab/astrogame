using System.Collections;
using System.Collections.Generic;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Sky Region Asset")]
    public sealed class SkyRegionBounds : NamedAsset {
        public EqCoords[] Boundaries;
    }
}
