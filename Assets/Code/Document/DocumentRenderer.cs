using System;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

namespace Astro {
    public sealed class DocumentRenderer : BatchedComponent {
        public TMP_Text Title;
        public TMP_Text FrontBodyText;
        public TMP_Text BackBodyText;
        public MeshRenderer Background;
        public VideoPlayer Video;
        public Transform FrontAnimation;
        public Vector3 ZoomOffsetOverride;
        public Rect Size;
        public DocumentInteractable Interactable;
    }
}