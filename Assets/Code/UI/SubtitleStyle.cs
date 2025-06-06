using TMPro;
using UnityEngine;
using System;
using System.Collections;

using FieldDay;
using FieldDay.UI;
using FieldDay.Vox;
using BeauRoutine;
using BeauUtil;
using UnityEngine.UI;
using Astro.Audio;
using FieldDay.Assets;

namespace Astro {
    [CreateAssetMenu(menuName = "Astro/Character Subtitle Style")]
    public sealed class SubtitleStyle : NamedAsset {
        [Header("Color")]
        public bool OverrideColors;
        [ShowIfField("OverrideColors")]
        public ColorPalette2 Colors = new ColorPalette2(Color.white, Color.black);

        [Header("Text")]
        public TMP_FontAsset OverrideFont;
        public float FontScale = 1;
        public float MarginScale = 1;

        [Header("Background")]
        public float BackgroundCornerRadiusScale = 1;
    }
}