using TMPro;
using UnityEngine;

using FieldDay;
using BeauUtil;
using FieldDay.Assets;

namespace Astro {
    [CreateAssetMenu(menuName = "Astro/Character Subtitle Style")]
    public sealed class SubtitleStyle : NamedAsset {
        [Header("Color")]
        public bool OverrideColors;
        [ShowIfField("OverrideColors")]
        public ColorPalette2 Colors = new ColorPalette2(Color.white, Color.black);
        public Color32 WaveformColor;

        [Header("Text")]
        public string DisplayName;
        public TMP_FontAsset OverrideFont;
        public float FontScale = 1;
        public float MarginScale = 1;

        [Header("Background")]
        public float BackgroundCornerRadiusScale = 1;
    }
}