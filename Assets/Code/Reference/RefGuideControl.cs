using System;
using BeauUtil;
using FieldDay.Components;
using UnityEngine;

namespace Astro.Reference {
    [RequireComponent(typeof(LabInteractable))]
    public sealed class RefGuideControl : BatchedComponent {
        public RefGuideControlType ControlType;
        [NonSerialized] public ReferenceClassification Classification;
    }

    public enum RefGuideControlType {
        PrevPage,
        NextPage,
        ToggleActive,
        Bookmark,
        Classification,
        Classification_Toggle,
    }
}