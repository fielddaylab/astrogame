using FieldDay;
using FieldDay.Components;
using UnityEngine;

namespace Astro {
    [RequireComponent(typeof(ParallaxGraphAnimation))]
    public class ParallaxGraph : BatchedComponent, IRegistrationCallbacks {
        public DataDisplay DistanceDisplay;
        public ParallaxGraphAnimation Graph;
        public void OnDeregister() {
        }

        public void OnRegister() {
        }
    }
}