using FieldDay.Scripting;
using FieldDay.UI.Animation;
using Leaf.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astro
{
    public class ScriptFader : ScriptActorComponent
    {
        [SerializeField] private FadeGroup Group;

        [LeafMember("FadeIn")]
        private void LeafFadeIn()
        {
            Group.Hide();
        }

        [LeafMember("FadeOut")]
        private void LeafFadeOut()
        {
            Group.Show();
        }
    }
}