using UnityEngine;

using Leaf.Runtime;
using FieldDay.Scripting;
using FieldDay.UI.Animation;

namespace Astro {
    public class ScriptFader : ScriptActorComponent {
        [SerializeField] private FadeGroup Group;

        [LeafMember("FadeIn")]
        private void LeafFadeIn() { Group.Hide(); }

        [LeafMember("FadeOut")]
        private void LeafFadeOut() { Group.Show(); }

        [LeafMember("CutToBlack")]
        private void LeafCutToBlack() { Group.SetVisibleNow(true); }
    }
}