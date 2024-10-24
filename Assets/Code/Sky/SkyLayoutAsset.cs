using System.Collections;
using System.Collections.Generic;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Sky Layout")]
    public sealed class SkyLayoutAsset : GlobalAsset {
        public CelestialAsset TestAsset;
    }
}
