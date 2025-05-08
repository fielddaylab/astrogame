using System;
using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Localization;
using FieldDay.SharedState;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    public sealed class RadioRig : SharedStateComponent, IRegistrationCallbacks {
        [Header("Controls")]
        public DialAdjustableInstrument Dial;
        
        [Header("Audio Sources")]
        public AudioSource StreamEmitter;
        public AudioSource StaticEmitter;

        [NonSerialized] public AudioHandle StaticAudioHandle;
        [NonSerialized] public AudioHandle StreamAudioHandle;

        [NonSerialized] public RadioChannel ClosestChannel;
        [NonSerialized] public float NormalizedChannelStrength;
        [NonSerialized] public int LastKnownFrequency;

        [NonSerialized] public RadioStaticMode StaticMode;
        [NonSerialized] public float StaticModeTimer;
        [NonSerialized] public float StaticVolume;

        void IRegistrationCallbacks.OnDeregister() {
            Sfx.Stop(StaticAudioHandle);
            Sfx.Stop(StreamAudioHandle);
        }

        void IRegistrationCallbacks.OnRegister() {
        }
    }

    public enum RadioStaticMode {
        Off,
        FadeIn,
        Tuning,
        FadeOut,
    }
}