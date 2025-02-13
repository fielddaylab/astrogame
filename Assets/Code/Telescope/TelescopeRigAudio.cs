using System;
using BeauRoutine;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
using FieldDay.Debugging;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class TelescopeRigAudio : SharedStateComponent {
        [Header("Dome")]
        [AudioEventRef] public StringHash32 DomeRotationLoop;
        public Transform DomeRotationLoopLocation;

        [Header("Base")]
        [AudioEventRef] public StringHash32 BaseRotationLoop;
        public Transform BaseRotationLoopLocation;

        [NonSerialized] public Vector2 LastKnownRotation;
        [NonSerialized] public AudioHandle DomeAudioHandle;
        [NonSerialized] public AudioHandle BaseAudioHandle;
    }

    static public partial class TelescopeUtility {
        
    }
}