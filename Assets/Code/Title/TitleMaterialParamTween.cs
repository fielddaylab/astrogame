using System;
using BeauRoutine;
using BeauUtil;
using FieldDay.Scripting;
using Leaf.Runtime;
using UnityEngine;

namespace Astro.Title {
    public sealed class TitleMaterialParamTween : ScriptActorComponent {
        public Material[] Materials;
        public Renderer[] Renderers;
        public TweenSettings FadeTween = new TweenSettings(1);
        public float FadeDelay;
        
        public string ParamId;
        public float Start;
        public float Target;

        [NonSerialized] public int ParamIdHash;
        [NonSerialized] public float[] OriginalParamValues;
        [NonSerialized] public Routine FadeRoutine;

        private void Awake() {
            ParamIdHash = Shader.PropertyToID(ParamId);

            OriginalParamValues = new float[Materials.Length];
            for (int i = 0; i < Materials.Length; i++) {
                OriginalParamValues[i] = Materials[i].GetFloat(ParamIdHash);
                Materials[i].SetFloat(ParamIdHash, Start);
            }

            foreach(var renderer in Renderers) {
                renderer.enabled = false;
            }
        }

        private void OnDestroy() {
            for (int i = 0; i < Materials.Length; i++) {
                Materials[i].SetFloat(ParamIdHash, OriginalParamValues[i]);
            }
        }

        [LeafMember("FadeIn")]
        public void FadeIn() {
            foreach (var renderer in Renderers) {
                renderer.enabled = true;
            }

            FadeRoutine.Replace(this, Tween.ZeroToOne(SetParamLerp, FadeTween).DelayBy(FadeDelay));
        }

        private void SetParamLerp(float lerp) {
            foreach (var material in Materials) {
                material.SetFloat(ParamIdHash, Start + (Target - Start) * lerp);
            }
        }
    }
}