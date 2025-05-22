using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using ScriptableBake;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Sky Layout")]
    public sealed class SkyLayoutAsset : GlobalAsset, IBaked {
        public TextAsset CelestialObJList;
        public CelestialAsset[] AllCelestialObjs;

#if UNITY_EDITOR

        [ContextMenu("Load Celestial Objs from CSV")]
        public void MenuLoadCelestialObjs()
        {
            // TODO: Parse celestial objs
            AllCelestialObjs = new CelestialAsset[] { };
        }

        int IBaked.Order => 100;

        bool IBaked.Bake(BakeFlags flags, BakeContext context) {
            HashSet<CelestialAsset> assets = new HashSet<CelestialAsset>(AllCelestialObjs);
            CelestialAsset[] newArr = new CelestialAsset[assets.Count];
            assets.CopyTo(newArr, 0);
            AllCelestialObjs = newArr;
            return true;
        }
#endif
    }
}
