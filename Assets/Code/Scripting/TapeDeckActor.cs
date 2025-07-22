
using FieldDay.Scripting;
using Leaf.Runtime;
using System;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(Animation)), RequireComponent(typeof(LabInstrument))]
    public class TapeDeckActor : ScriptActorComponent {
        [NonSerialized] Animation Anim;
        [NonSerialized, HideInInspector] LabInstrument TapeDeck;

        public override void OnScriptRegister(ScriptActor actor) {
            base.OnScriptRegister(actor);
            Anim = GetComponent<Animation>();
            TapeDeck = GetComponent<LabInstrument>();
        }

        [LeafMember("SetTapeActive")]
        public void SetTapeActive(bool active) {
            if (active) {
                TapeDeck.Unlocked = true;
                TapeDeck.OnUnlock?.Invoke(TapeDeck);

                Anim.Play();
            } else {
                TapeDeck.Unlocked = false;
                TapeDeck.OnUnlock?.Invoke(TapeDeck);

                Anim.Stop();
            }
        }
    }
}