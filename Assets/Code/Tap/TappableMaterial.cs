using BeauUtil;
using FieldDay.Assets;
using FieldDay.Audio;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "Astro/Tappable Material")]
    public sealed class TappableMaterial : NamedAsset {
        [AudioEventRef] public StringHash32 Sound;
    }
}