using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleMaterialFadeGroup : ScriptActorComponent {
        public Material[] Materials;
        public Renderer[] Renderers;
        public TweenSettings FadeTween = new TweenSettings(1);
        public float FadeDelay;

        [NonSerialized] public Color[] OriginalColors;
        [NonSerialized] public Routine FadeRoutine;

        private void Awake() {
            OriginalColors = new Color[Materials.Length];

            for (int i = 0; i < Materials.Length; i++) {
                OriginalColors[i] = Materials[i].color;
                Materials[i].color = Color.black;
            }

            foreach(var renderer in Renderers) {
                renderer.enabled = false;
            }
        }

        private void OnDestroy() {
            for (int i = 0; i < Materials.Length; i++) {
                Materials[i].color = OriginalColors[i];
            }
        }

        [LeafMember("FadeIn")]
        public void FadeIn() {
            foreach (var renderer in Renderers) {
                renderer.enabled = true;
            }
            FadeRoutine.Replace(this, Tween.ZeroToOne(SetColorLerp, FadeTween).DelayBy(FadeDelay));
        }

        private void SetColorLerp(float lerp) {
            for (int i = 0; i < Materials.Length; i++) {
                Materials[i].color = OriginalColors[i] * lerp;
            }
        }
    }
}