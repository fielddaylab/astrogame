using BeauUtil;
using EasyAssetStreaming;
using FieldDay.Assets;
using FieldDay.Audio;
using UnityEngine;

namespace Astro.Radio {
    [CreateAssetMenu(menuName = "Astro/Radio Channel")]
    public sealed class RadioChannel : NamedAsset {
        public RadioChannelMode Mode = RadioChannelMode.Loop;
        
        [Header("Location")]
        [Range(100, 865)] public int Frequency = 500;
        [Range(0, 50)] public int TuningRange = 20;

        [Header("Audio")]
        [StreamingAudioPath] public string AudioStream;
        public AudioClip AudioClip;
        [AudioEventRef] public StringHash32 EventOverride;
    }

    public enum RadioChannelMode {
        OneShot,
        Loop,
        Scripted
    }
}