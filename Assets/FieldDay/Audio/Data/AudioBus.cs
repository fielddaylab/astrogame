using System;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Filters;
using UnityEngine;

namespace FieldDay.Audio {
    /// <summary>
    /// Audio bus information.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Audio Bus", order = -279)]
    public sealed class AudioBus : NamedAsset {
        [AudioBusId] public StringHash32 ParentId;

        [Inline(InlineAttribute.DisplayType.HeaderLabel)]
        public AudioPropertyBlock Properties = AudioPropertyBlock.Default;

        [Header("Ducking")]
        [AudioMixStateRef] public StringHash32 DuckingMix;
        public SignalEnvelope DuckingEnvelope = new SignalEnvelope(1, 1);

        #region Lookup

        static public readonly StringHash32 Master = "Master";

        #endregion // Lookup
    }

    public sealed class AudioBusIdAttribute : AssetNameAttribute {
        public AudioBusIdAttribute() : base(typeof(AudioBus), true) {
            DropdownNullName = "[Master]";
        }
    }
}