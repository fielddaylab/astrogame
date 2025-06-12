using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleParticlePlayer : ScriptActorComponent {
        public ParticleSystem[] Systems;

        [LeafMember("PlayEffects")]
        public void PlayEffects() {
            foreach(var system in Systems) {
                system.Play();
            }
        }
    }
}