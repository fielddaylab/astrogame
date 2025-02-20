using System;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using TMPro;
using UnityEngine;

namespace Astro {
    public sealed class DocumentRenderer : BatchedComponent {
        public TMP_Text Title;
        public TMP_Text Body;
        public MeshRenderer Background;
        public Vector3 ZoomOffsetOverride;
        public Rect Size;
    }
}