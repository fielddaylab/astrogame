using BeauUtil;
using FieldDay.Assets;
using FieldDay.Audio;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "Astro/Tappable Material")]
    public sealed class TappableMaterial : NamedAsset {
        [AudioEvent] public StringHash32 Sound;
    }
}