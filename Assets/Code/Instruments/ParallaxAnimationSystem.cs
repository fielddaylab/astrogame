using FieldDay.Systems;
using System;
using UnityEngine;

namespace Astro.Reference {

    public class ParallaxAnimationSystem : ComponentSystemBehaviour<ParallaxAnimation> {
        public override void ProcessWorkForComponent(ParallaxAnimation component, float deltaTime) {
            component.OrbitTimer.Advance(deltaTime);
            OrbitEarth(component);
        }

        private void OrbitEarth(ParallaxAnimation component) {
            float xPos = component.OrbitRadius * (float)Math.Cos(2 * Math.PI * component.OrbitTimer.Accumulator / component.OrbitTimer.Period);
            component.EarthSprite.localPosition += new Vector3(xPos, 0, 0);
        }
    }
}