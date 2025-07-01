
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Animation))]
    public class TapeDeckActor : ScriptActorComponent {
        [NonSerialized] Animation Anim;

        public override void OnScriptRegister(ScriptActor actor) {
            base.OnScriptRegister(actor);
            Anim = GetComponent<Animation>();

        }

        [LeafMember("SetTapeActive")]
        public void SetTapeActive(bool active) {
            if (active) {
                Anim.Play();
            } else {
                Anim.Stop();
            }
        }
    }
}