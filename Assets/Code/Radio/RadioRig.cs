using FieldDay.SharedState;
using FieldDay.Vox;
using UnityEngine;

namespace Astro.Radio {
    public sealed class RadioRig : SharedStateComponent {
        public AudioSource BypassEmitter;
        public AudioSource StreamEmitter;
        public AudioSource StaticEmitter;
    }
}