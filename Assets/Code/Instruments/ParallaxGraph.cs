using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(ParallaxGraphAnimation))]
    public class ParallaxGraph : BatchedComponent {
        public DataDisplay DistanceDisplay;
        public ParallaxGraphAnimation Graph;
    }
}