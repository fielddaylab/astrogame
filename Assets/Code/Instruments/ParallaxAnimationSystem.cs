using BeauRoutine.Splines;
using EasyAssetStreaming;
using FieldDay.Systems;
using System;
using System.ComponentModel;
using UnityEngine;

namespace Astro {
    public class ParallaxAnimationSystem : ComponentSystemBehaviour<ParallaxGraphAnimation> {
        public override void ProcessWorkForComponent(ParallaxGraphAnimation component, float deltaTime) {
            if (component.LinkedDistance.CurrentData.Value.Distance <= 0) {
                return;
            }
            AdvanceOrbitCos(component, deltaTime);
        }

        private void AdvanceOrbitCos(ParallaxGraphAnimation anim, float deltaTime) {
            anim.OrbitTimer.Advance(deltaTime);
            float progress = anim.OrbitTimer.Accumulator / anim.OrbitTimer.Period;
            float offset = anim.OrbitRadius * (float)Math.Cos(2 * Math.PI * progress);
            Vector3 offset3 = anim.OrbitInX ? new Vector3(offset, 0, 0) : new Vector3(0, offset, 0);
            anim.EarthSprite.localPosition = anim.EarthInitPos + offset3;
            if (progress < 0.5) {
                anim.EarthTex.SortingOrder = -2;
            } else {
                anim.EarthTex.SortingOrder = 2;
            }
            anim.StarImageSprite.localPosition = anim.StarInitPos + (offset3 * anim.ParallaxFactor);
            ParallaxDataUtility.UpdateSpline(anim);
            }
    }
}