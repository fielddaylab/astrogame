using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using FieldDay.SharedState;

namespace Astro
{
    public class ColorTextureState : SharedStateComponent
    {
        [Header("Indicators")]
        public Material IndicatorInactive;
        public Material IndicatorActive;

        [Header("Panels")]
        public Material PanelBlank;
    }
}