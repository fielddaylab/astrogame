using BeauRoutine.Splines;
using FieldDay.Systems;
using System;
using UnityEngine;

namespace Astro {

    public class ParallaxAnimationSystem : ComponentSystemBehaviour<ParallaxAnimation> {
        public override void ProcessWorkForComponent(ParallaxAnimation component, float deltaTime) {
            if (component.LinkedDistance.CurrentData.Value.Distance <= 0) {
                return;
            }
            //AdvanceOrbitTime(component, deltaTime);
            AdvanceOrbitSpeedY(component, deltaTime);
        }

        private void AdvanceOrbitTime(ParallaxAnimation component, float deltaTime) {
            if (component.OrbitTimer.Advance(deltaTime)) {
                component.EarthSprite.localPosition = component.EarthInitPos;
            } else {
                float pos = (float)Math.Cos(2 * Math.PI * component.OrbitTimer.Accumulator / component.OrbitTimer.Period);
                component.EarthSprite.localPosition += component.OrbitInX ? new Vector3(pos, 0, 0) : new Vector3(0, pos, 0);
            }
        }

        private void AdvanceOrbitSpeedY(ParallaxAnimation component, float deltaTime) {
            float earthDistance = Math.Abs(component.EarthSprite.localPosition.y - component.EarthInitPos.y); 
            if (earthDistance > component.OrbitRadius) {
                component.EarthSprite.localPosition = new Vector3(0, Math.Sign(component.OrbitVelocity) * component.OrbitRadius, 0);
                component.OrbitVelocity *= -1;
            } else {
                component.EarthSprite.localPosition += new Vector3(0, component.OrbitVelocity * deltaTime, 0);
            }
            component.StarImageSprite.localPosition += new Vector3(0, component.OrbitVelocity * component.GetParallaxFactor() * deltaTime, 0);
            ParallaxAnimationUtility.UpdateSpline(component);
        }

        private void AdvanceOrbitSpeedX(ParallaxAnimation component, float deltaTime) {
            float earthDistance = Math.Abs(component.EarthSprite.localPosition.x - component.EarthInitPos.x);
            if (earthDistance > component.OrbitRadius) {
                component.EarthSprite.localPosition = new Vector3(Math.Sign(component.OrbitVelocity) * component.OrbitRadius, 0, 0);
                component.OrbitVelocity *= -1;
            } else {
                component.EarthSprite.localPosition += new Vector3(component.OrbitVelocity * deltaTime, 0, 0);
            }
            component.StarImageSprite.localPosition += new Vector3(component.OrbitVelocity * component.GetParallaxFactor() * deltaTime, 0, 0);
            ParallaxAnimationUtility.UpdateSpline(component);
        }



    }

    public static class ParallaxAnimationUtility {

        public static void OnDistanceUpdate(DataPacket packet, ParallaxAnimation anim) {
            if ((packet.Type & DataTypeMask.Distance) != 0) {
                SetParallaxFactor((float) packet.Value.Distance, anim);
            } else {
                ResetAnimation(anim);
            }
        }

        public static void UpdateSpline(ParallaxAnimation anim) {
            anim.Spline.SetVertex(0, anim.EarthSprite.position);
            anim.Spline.SetVertex(1, anim.StarImageSprite.position);
        }

        public static void SetParallaxFactor(float distance, ParallaxAnimation anim) {
            ResetAnimation(anim);
            if (distance <= 0) {
                return;
            }
            anim.SetParallaxFactor(-10 /distance);
            anim.Ruler.localScale = anim.OrbitInX ? 
                new Vector3(2, -2*anim.GetParallaxFactor() * anim.OrbitRadius, 1) : 
                new Vector3(-2 * anim.GetParallaxFactor() * anim.OrbitRadius, 2, 1);
            anim.RealStarSprite.localPosition = new Vector3(-800/distance + 140, 0, 0);
            anim.Ruler.gameObject.SetActive(true);
            anim.Spline.gameObject.SetActive(true);
            UpdateSpline(anim);
        }

        public static void ResetAnimation(ParallaxAnimation anim) {
            anim.StarImageSprite.localPosition = anim.StarInitPos;
            anim.EarthSprite.localPosition = anim.EarthInitPos;
            UpdateSpline(anim);
            anim.Ruler.gameObject.SetActive(false);
            anim.Spline.gameObject.SetActive(false);
        }
    }
}