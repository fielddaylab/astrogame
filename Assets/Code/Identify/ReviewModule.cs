
using BeauUtil;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    public class ReviewModule : BatchedComponent {
        [SerializeField] public SpriteRenderer[] CountdownSprites;
        [SerializeField] public SpriteRenderer ResultSprite;

        [SerializeField] public TMP_Text PointsDisplay;
        [HideInInspector] public int PipsRevealed;

    }

    public static partial class PointsUtility {
        public static void UpdatePointDisplay(PlayerPointsState state) {
            state.ReviewModule.PointsDisplay.SetText(state.SciencePoints.ToStringLookup());
        }
    }
}