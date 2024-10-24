using System.Collections;
using System.Collections.Generic;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Sky Layout")]
    public sealed class SkyLayoutAsset : GlobalAsset {
        public TextAsset CelestialObJList;
        public CelestialAsset[] AllCelestialObjs;

#if UNITY_EDITOR

        [ContextMenu("Load Celestial Objs from CSV")]
        public void MenuLoadCelestialObjs()
        {
            // TODO: Parse celestial objs
            AllCelestialObjs = new CelestialAsset[] { };
        }
#endif
    }
}
