using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class TelescopeAnimator : BatchedComponent {
        public Transform AimPivot;
        public bool AutoSync;
    }
}