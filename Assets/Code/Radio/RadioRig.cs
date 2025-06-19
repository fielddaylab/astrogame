using BeauUtil;
using BeauUtil.Debugger;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Localization;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf.Runtime;
using System;
using System.Net.Sockets;
using UnityEngine;

namespace Astro.Radio {
    public sealed class RadioRig : SharedStateComponent, IRegistrationCallbacks {
        [Header("Controls")]
        public DialAdjustableInstrument Dial;
        public LabButton Power;
        
        [Header("Audio Sources")]
        public AudioSource StreamEmitter;
        public AudioSource StaticEmitter;

        [Header("Mixes")]
        [AudioMixStateRef] public StringHash32 MixId;

        [NonSerialized] public AudioHandle StaticAudioHandle;
        [NonSerialized] public AudioHandle StreamAudioHandle;

        [NonSerialized] public RadioChannel ClosestChannel;
        [NonSerialized] public float NormalizedChannelStrength;
        [NonSerialized] public int LastKnownFrequency;

        [NonSerialized] public RadioStaticMode StaticMode;
        [NonSerialized] public float StaticModeTimer;
        [NonSerialized] public float StaticVolume;

        [NonSerialized] public bool IsLocked;

        void IRegistrationCallbacks.OnDeregister() {
            Sfx.Stop(StaticAudioHandle);
            Sfx.Stop(StreamAudioHandle);
        }

        void IRegistrationCallbacks.OnRegister() {
            Dial.Source.CanAdjust = (dial, delta) => {
                if (IsLocked) {
                    // TODO: Play locked disconnect
                    return false;
                }

                return true;
            };
        }
    }

    public enum RadioStaticMode {
        Off,
        FadeIn,
        Tuning,
        FadeOut,
    }

    static public partial class RadioUtility {
        [LeafMember("SetRadioDialLocked")]
        static public void SetRadioLocked(bool locked) {
            Find.State<RadioRig>().IsLocked = locked;
        }

        [LeafMember("SetRadioFrequency")]
        static public void SnapRadioFrequency(int frequency) {
            var state = Find.State<RadioRig>();
            InstrumentUtility.TrySetValue(state.Dial, frequency);
        }

        [LeafMember("SnapRadioToChannel")]
        static public void SnapRadioFrequencyToChannel() {
            var state = Find.State<RadioRig>();
            if (state.ClosestChannel != null) {
                InstrumentUtility.TrySetValue(state.Dial, state.ClosestChannel.Frequency);
            }
        }
    }
}