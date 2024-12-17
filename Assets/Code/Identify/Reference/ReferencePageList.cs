using FieldDay.Assets;
using System.Collections.Generic;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Reference/PageList")]
    public class ReferencePageList : GlobalAsset {
        public List<ReferencePageAsset> Pages;
    }
}