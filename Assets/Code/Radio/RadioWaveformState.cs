using Astro.Audio;
using BeauUtil;
using FieldDay;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Vox;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Astro.Radio {
    public sealed class RadioWaveformState : SharedStateComponent, IScenePreload {
        public Material WaveformMaterial;

        [Header("Noise")]
        public Color32 NoiseColor;
        public float NoiseScale = 0.4f;

        [Header("Channel")]
        public Color32 ChannelColor;
        public float ChannelScale = 0.4f;

        [Header("Voice")]
        public float VoiceScale = 0.4f;
        public VoxEmitter[] Voices;

        [NonSerialized] public float CurrentScale;
        [NonSerialized] public Color CurrentColor;
        [NonSerialized] public float CurrentLerp;

        [NonSerialized] public VoxWaveformTable VoxWaveformTable;
        [NonSerialized] public VoxWaveformTable RadioWaveformTable;

        IEnumerator<WorkSlicer.Result?> IScenePreload.Preload() {
            CurrentScale = 0;
            CurrentColor = NoiseColor;
            CurrentLerp = 0;

            VoxWaveformTable = Find.NamedAsset<VoxWaveformTable>("VoxTable");
            RadioWaveformTable = Find.NamedAsset<VoxWaveformTable>("RadioTable");
            return null;
        }
    }
}