
using BeauUtil;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    public class ReviewModule : BatchedComponent {
        [SerializeField] public SpriteRenderer[] CountdownSprites;
        [SerializeField] private SpriteRenderer ResultSprite;

        [SerializeField] public TMP_Text PointsDisplay;

    }

    public static partial class PointsUtility {
        public static void UpdatePointDisplay(PlayerPointsState state) {
            state.ReviewModule.PointsDisplay.SetText(state.SciencePoints.ToStringLookup());
        }


    }
}