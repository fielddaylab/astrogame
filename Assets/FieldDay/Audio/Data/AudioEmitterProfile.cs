using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio emitter profile.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio Emitter Profile", order = -278)]
    public sealed class AudioEmitterProfile : NamedAsset {
        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public AudioEmitterConfig Config = AudioEmitterConfig.Default3D;
    }
}