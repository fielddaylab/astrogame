using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio emitter profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Emitter Profile")]
    public sealed class AudioEmitterProfile : NamedAsset {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public AudioEmitterConfig Config = AudioEmitterConfig.Default3D;
    }
}