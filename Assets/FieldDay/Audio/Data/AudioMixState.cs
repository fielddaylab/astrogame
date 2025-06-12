using System;
using BeauRoutine.Extensions;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Filters;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio mix information.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio Mix State", order = -277)]
    public sealed class AudioMixState : NamedAsset, IRegistrationCallbacks {
        [Serializable]
        internal struct BusMix {
            [AudioBusId] public StringHash32 Bus;
            [Range(0, 1)] public float Volume;
            [Range(-3, 3)] public float Pitch;
            [Range(-1, 1)] public float Pan;
            [Range(0, 1)] public float LoPass;
            [Range(0, 1)] public float HiPass;
        }

        [SerializeField] internal BusMix[] Mixes;
        [SerializeField] public SignalEnvelope DefaultEnvelope = new SignalEnvelope(1, 1);
        
        [NonSerialized] internal StringHash32 CachedId;
        [NonSerialized] internal AudioMixBlock MixBlock;
        [NonSerialized] internal bool Linked;

        void IRegistrationCallbacks.OnDeregister() {
        }

        void IRegistrationCallbacks.OnRegister() {
            CachedId = name;
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode || !Frame.IsActive(this)) {
                return;
            }

            AudioMgr.ReloadAudioMixState(this);
        }
#endif // UNITY_EDITOR
    }

    /// <summary>
    /// Mix snapshot reference attribute.
    /// </summary>
    public class AudioMixStateRefAttribute : AssetNameAttribute {
        public AudioMixStateRefAttribute() : base(typeof(AudioMixState), true) { }

        protected internal override string Name(UnityEngine.Object obj) {
            return base.Name(obj).Replace('-', '/').Replace('.', '/');
        }
    }
}