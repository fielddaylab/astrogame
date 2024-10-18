using System;
using BeauUtil;
using FieldDay.Assets;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(DocumentRenderer))]
    public sealed class DocumentState : BatchedComponent {
        [NonSerialized] public DocumentRenderer Renderer;
    }
}