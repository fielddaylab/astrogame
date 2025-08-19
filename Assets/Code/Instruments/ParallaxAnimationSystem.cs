
using System;
using UnityEngine;
using FieldDay.Systems;

namespace Astro {
    public class ParallaxAnimationSystem : ComponentSystemBehaviour<ParallaxGraphAnimation> {
        public override void ProcessWorkForComponent(ParallaxGraphAnimation component, float deltaTime) {
            if (component.LinkedDistance.CurrentData.Value.Distance <= 0) return;

            AdvanceOrbitCos(component, deltaTime);
        }

        private void AdvanceOrbitCos(ParallaxGraphAnimation anim, float deltaTime) {
            anim.OrbitTimer.Advance(deltaTime);
            float progress = anim.OrbitTimer.Accumulator / anim.OrbitTimer.Period;
            float offset = anim.OrbitRadius * (float)Math.Cos(2 * Math.PI * progress);
            Vector3 offset3 = anim.OrbitInX ? new Vector3(offset, 0, 0) : new Vector3(0, offset, 0);
            anim.EarthSprite.transform.localPosition = anim.EarthInitPos + offset3;
            anim.EarthSprite.sortingOrder = (progress > 0.5f) ? -2 : 2;
            anim.StarImageSprite.transform.localPosition = anim.StarInitPos + (offset3 * anim.ParallaxFactor);
            ParallaxDataUtility.UpdateSpline(anim);
        }
    }
}