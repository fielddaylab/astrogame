using System;
using BeauUtil;
using FieldDay.Audio;
using Leaf.Runtime;
using UnityEngine;

namespace FieldDay.Scripting.Components {
    [RequireComponent(typeof(SfxNode))]
    public sealed class ScriptSfxNode : ScriptActorComponent {
        [NonSerialized] private SfxNode m_Node;

        [LeafMember("Play")]
        public void Play() {
            this.CacheComponent(ref m_Node).Play();
        }


        [LeafMember("Stop")]
        public void Stop() {
            this.CacheComponent(ref m_Node).Stop();
        }
    }
}