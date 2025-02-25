
using System;
using BeauUtil;
using FieldDay.Components;
using FieldDay.Rendering;
using TMPro;
using UnityEngine;

namespace Astro {
    public class ReviewModule : BatchedComponent {
        [Header("Countdown")]
        [SerializeField] public MeshRenderer[] CountdownSprites;
        [SerializeField] public MeshRenderer Result;
        
        [Header("Points Output")]
        [SerializeField] public TMP_Text PointsDisplay;
        [SerializeField] public RenderAtlasOutput PointsOutput;

        [Header("Materials")]
        public Material UnlitPipMaterial;
        public Material LitPipMaterial;
        public Material SuccessMaterial;
        public Material FailureMaterial;

        [NonSerialized] public int PipsRevealed;

    }

    public static partial class PointsUtility {
        public static void UpdatePointDisplay(PlayerPointsState state) {
            state.ReviewModule.PointsDisplay.SetText(state.SciencePoints.ToStringLookup());
            state.ReviewModule.PointsOutput.MarkDirty();
        }
    }

    public static class ReviewModuleUtility {
        public static void SetPipReadout(ReviewModule module, int numPips) {
            module.PipsRevealed = numPips;

            for (int i = 0; i < module.CountdownSprites.Length; i++) { 
                if (i < numPips){ 
                    module.CountdownSprites[i].SetSharedMaterialAtIndex(1, module.LitPipMaterial);
                }else{
                    module.CountdownSprites[i].SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                }
            }
        }

        public static void ShowResultSprite(bool correct, ReviewModule module) {
            module.Result.SetSharedMaterialAtIndex(1, correct ? module.SuccessMaterial : module.FailureMaterial);
        }
    }
}