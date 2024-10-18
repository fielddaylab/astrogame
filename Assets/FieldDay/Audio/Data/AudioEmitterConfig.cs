using System;
using BeauRoutine;
using BeauUtil;
using UnityEngine;

namespace FieldDay.Audio {

    /// <summary>
    /// Control spatialized audio emission.
    /// </summary>
    [Serializable]
    public struct AudioEmitterConfig {
        /// <summary>
        /// How this emitter is positioned.
        /// </summary>
        [AutoEnum] public AudioEmitterMode Mode;

        [Header("Attenuation")]

        /// <summary>
        /// Rolloff type.
        /// </summary>
        [Tooltip("How audio will roll off over distance.")]
        [AutoEnum] public AudioRolloffMode Rolloff;

        /// <summary>
        /// Custom rolloff curve.
        /// </summary>
        [Tooltip("Custom rolloff curve")]
        public AnimationCurve CustomRolloffCurve;

        /// <summary>
        /// Rolloff minimum distance.
        /// </summary>
        [Tooltip("Minimum rolloff distance")]
        [Range(0, 300)] public float MinDistance;

        /// <summary>
        /// Rolloff maximum distance.
        /// </summary>
        [Tooltip("Maximum rolloff distance")]
        [Range(0, 300)] public float MaxDistance;

        [Header("Spatialization")]

        /// <summary>
        /// Factor by which the audio is "despatialized".
        /// </summary>
        [Tooltip("Adjusts the impact of positioning on playback.\n0 = Full 3D, 1 = Completely Flat")]
        [Range(0, 1)] public float DespatializeFactor;

        /// <summary>
        /// Spread angle of sound.
        /// </summary>
        [Range(0, 360)]
        public float Spread;

        [Header("Effects")]

        /// <summary>
        /// Doppler level of sound.
        /// </summary>
        [Range(0, 5)]
        public float DopplerLevel;

        /// <summary>
        /// Wet mix applied for ReverbZones.
        /// </summary>
        [Range(0, 1.1f)]
        public float ReverbZoneMix;

        /// <summary>
        /// Effect bypass flags.
        /// </summary>
        [Tooltip("Adjusts what effects, if any, are bypassed.")]
        [AutoEnum] public AudioEmitterBypassFlags EffectBypasses;

        /// <summary>
        /// Default non-spatial playback.
        /// </summary>
        static public readonly AudioEmitterConfig Default2D = new AudioEmitterConfig() {
            Mode = AudioEmitterMode.Fixed,
            Rolloff = AudioRolloffMode.Logarithmic,
            MinDistance = 1,
            MaxDistance = 500,
            DespatializeFactor = 1,
            EffectBypasses = AudioEmitterBypassFlags.ReverbZones
        };

        /// <summary>
        /// Default spatial playback.
        /// </summary>
        static public readonly AudioEmitterConfig Default3D = new AudioEmitterConfig() {
            Mode = AudioEmitterMode.World,
            Rolloff = AudioRolloffMode.Logarithmic,
            MinDistance = 1,
            MaxDistance = 500,
            DespatializeFactor = 0,
            DopplerLevel = 1,
            ReverbZoneMix = 1,
        };

        #region Utility

        /// <summary>
        /// Applies the given audio emitter configuration to the given audio source.
        /// </summary>
        static public void ApplyConfiguration(AudioSource source, in AudioEmitterConfig config, bool hasSpatializationPlugin) {
            source.bypassListenerEffects = (config.EffectBypasses & AudioEmitterBypassFlags.ListenerEffects) != 0;
            source.bypassReverbZones = (config.EffectBypasses & AudioEmitterBypassFlags.ReverbZones) != 0;
            source.bypassEffects = (config.EffectBypasses & AudioEmitterBypassFlags.LocalEffects) != 0;

            source.reverbZoneMix = config.ReverbZoneMix;

            switch (config.Mode) {
                case AudioEmitterMode.Fixed: {
                    source.spatialBlend = 0;
                    source.spatialize = false;
                    break;
                }

                default: {
                    source.dopplerLevel = config.DopplerLevel;
                    source.spread = config.Spread;
                    source.rolloffMode = config.Rolloff;
                    if (config.Rolloff == AudioRolloffMode.Custom) {
                        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, config.CustomRolloffCurve);
                    }
                    source.minDistance = config.MinDistance;
                    source.maxDistance = config.MaxDistance;
                    source.spatialBlend = 1 - config.DespatializeFactor;
                    source.spatialize = hasSpatializationPlugin && (config.EffectBypasses & AudioEmitterBypassFlags.SpatializationPlugin) == 0;
                    break;
                }
            }
        }

        #endregion // Utility
    }

    /// <summary>
    /// Bypass flags
    /// </summary>
    [Flags]
    public enum AudioEmitterBypassFlags {
        LocalEffects = 0x01,
        ListenerEffects = 0x02,
        ReverbZones = 0x04,
        SpatializationPlugin = 0x08
    }

    /// <summary>
    /// How sounds are positioned.
    /// </summary>
    [LabeledEnum(false)]
    public enum AudioEmitterMode : byte {
        [Label("Fixed (Not Spatial)")]
        Fixed,

        [Label("2D/XY")]
        Flat,

        [Label("2D/XZ")]
        FlatXZ,

        [Label("2D/YZ")]
        FlatYZ,

        [Label("3D/World")]
        World,

        [Label("3D/Relative to Listener")]
        ListenerRelative,

        [Label("2D/Screen Space")]
        ScreenSpace,

        [Label("Custom/A")]
        CustomA,

        [Label("Custom/B")]
        CustomB
    }
}