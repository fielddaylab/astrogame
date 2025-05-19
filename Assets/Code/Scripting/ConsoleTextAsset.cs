using System;
using FieldDay.Assets;
using FieldDay.Components;
using FieldDay.Scripting;
using TMPro;
using UnityEngine;

namespace Astro {
    [CreateAssetMenu(menuName = "AstroGame/Console Text Asset")]
    public sealed class ConsoleTextAsset : NamedAsset {
        [Serializable]
        public struct LineInfo {
            public string Text;
            public bool IsKeyboard;
            public float DelayAfter;
        }

        public LineInfo[] Lines;
    }
}