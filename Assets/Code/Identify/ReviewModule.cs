
using System;
using BeauUtil;
using FieldDay.Components;
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
}