using FieldDay.Components;
using UnityEngine;

namespace Astro.Reference {
    [RequireComponent(typeof(LabInteractable))]
    public sealed class RefGuideControl : BatchedComponent {
        public RefGuideControlType ControlType;
    }

    public enum RefGuideControlType {
        PrevPage,
        NextPage,
        ToggleActive,
        Bookmark,
        Classification
    }
}