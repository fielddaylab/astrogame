using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.Playables;

namespace Astro.Title {
    public sealed class TitleTimelinePlayer : ScriptActorComponent {
        public PlayableDirector Director;

        [LeafMember("PlayTimeline")]
        public void PlayTimeline() {
            Director.Play();
        }
    }
}