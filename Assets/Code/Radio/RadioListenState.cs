using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Debugger;
using EasyAssetStreaming;
using FieldDay;
using FieldDay.Assets;
using FieldDay.Audio;
using FieldDay.Scenes;
using FieldDay.SharedState;
using FieldDay.Vox;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Networking;

namespace Astro.Radio {
    public sealed class RadioListenState : SharedStateComponent {
        public float ListenTimeThreshold; // how long before the player is considered "listening"
        public float ListenStrengthThreshold; // how tuned-in the player must be to be considered "listening"
        public RadioChannel CurrListenChannel;
        public bool ActivelyListening;
        [NonSerialized] public float ListenTime; // how long the player has been listening to the current channel
    }
}