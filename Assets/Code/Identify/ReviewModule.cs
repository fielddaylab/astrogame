
using System;
using BeauUtil;
using FieldDay;
using FieldDay.Audio;
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

        [Header("Sound Effects")]
        [AudioEventRef] public StringHash32[] PipCountSounds;
        public Transform SoundAnchor;

        [Header("Materials")]
        public Material UnlitPipMaterial;
        public Material LitPipMaterial;
        public Material SuccessMaterial;
        public Material FailureMaterial;

        [NonSerialized] public int PipsRevealed;
        [NonSerialized] public bool ResultShown;

    }

    public static partial class ReviewUtility {
        public static void UpdatePointDisplay(ReviewModule reviewModule, PlayerPointsState points) {
            reviewModule.PointsDisplay.SetText(points.SciencePoints.ToStringLookup());
        }
    }

    public static class ReviewModuleUtility {
        public static void SetPipReadout(ReviewModule module, int numPips) {
            if (module.PipsRevealed != numPips) {
                module.PipsRevealed = numPips;
                Sfx.PlayDetached(module.PipCountSounds[numPips], module.SoundAnchor);
            }

            for (int i = 0; i < module.CountdownSprites.Length; i++) { 
                if (i < numPips){ 
                    module.CountdownSprites[i].SetSharedMaterialAtIndex(1, module.LitPipMaterial);
                }else{
                    module.CountdownSprites[i].SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
                }
            }
        }

        public static void ShowResultSprite(bool correct, ReviewModule module) {
            if (module.ResultShown) {
                return;
            }

            module.Result.SetSharedMaterialAtIndex(1, correct ? module.SuccessMaterial : module.FailureMaterial);
            module.ResultShown = true;

            if (correct) {
                Sfx.PlayDetached("Oneshot.Review.Success", module.SoundAnchor);
            } else {
                Sfx.PlayDetached("Oneshot.Review.Failure", module.SoundAnchor);
            }
        }

        public static void ResetReview(ReviewModule module = null) {
            if (module == null) {
                module = Find.State<ReviewState>().ReviewModule;
            }

            module.PipsRevealed = 0;
            module.ResultShown = false;
            foreach (MeshRenderer pip in module.CountdownSprites) {
                pip.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);
            }
            module.Result.SetSharedMaterialAtIndex(1, module.UnlitPipMaterial);

            ReviewState review = Find.State<ReviewState>();
            review.CurrentSubmission = ReviewSubmissionType.None;
            review.ReviewTimer.Paused = false;
        }
    }
}