using System;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay.Assets;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio event information.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio/Audio Event")]
    public sealed class AudioEvent : NamedAsset, IRegistrationCallbacks {
        public AudioClip[] Samples = Array.Empty<AudioClip>();

        [Header("Playback Parameters")]
        public FloatRange Volume = new FloatRange(1);
        public FloatRange Pitch = new FloatRange(1);
        public FloatRange Pan = new FloatRange(0);
        public FloatRange Delay = new FloatRange(0);
        [Space]
        public bool Loop;
        public bool RandomizeStartTime;

        [Header("Other Parameters")]
        [Range(0, 256)] public byte Priority = 128;
        [AssetName(typeof(AudioEmitterProfile))] public StringHash32 EmitterConfiguration;
        public SerializedHash32 Tag;

        [NonSerialized] internal StringHash32 CachedId;
        [NonSerialized] internal AudioEmitterProfile CachedEmitterProfile;
        [NonSerialized] internal RandomDeck<AudioClip> SampleSelector;
        
        /// <summary>
        /// Returns if this is a valid event.
        /// </summary>
        public bool IsValid() {
            return Samples.Length > 0;
        }

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            CachedId = name;
        }
    }

    /// <summary>
    /// Event reference attribute.
    /// </summary>
    public class AudioEventRefAttribute : AssetNameAttribute {
        public AudioEventRefAttribute() : base(typeof(AudioEvent), true) { }

        protected internal override string Name(UnityEngine.Object obj) {
            return base.Name(obj).Replace('-', '/').Replace('.', '/');
        }
    }
}