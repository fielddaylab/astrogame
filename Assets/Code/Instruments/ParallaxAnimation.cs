using Astro;
using BeauRoutine.Splines;
using FieldDay;
using FieldDay.Components;
using System;
using UnityEngine;

namespace Astro {
    public class ParallaxAnimation : BatchedComponent, IRegistrationCallbacks {
        #region Inspector
        [Header("Inspector")]
        public DataSlot LinkedDistance;
        public Transform SunSprite;
        public Transform EarthSprite;
        public Transform StarImageSprite;
        public Transform RealStarSprite;
        public Transform Ruler;
        public MultiSpline Spline;

        [Header("Orbit Settings")]
        public bool OrbitInX;
        public float OrbitRadius;
        public Timer OrbitTimer;
        public float OrbitVelocity;
        private float ParallaxFactor;


        [NonSerialized] public float ScaleFactor;
        [NonSerialized] public Vector3 EarthInitPos;
        [NonSerialized] public Vector3 StarInitPos;

        public void OnDeregister() {
        }

        public void OnRegister() {
            ScaleFactor = transform.localScale.y;
            EarthInitPos = EarthSprite.localPosition;
            StarInitPos = StarImageSprite.localPosition;
            Spline.SetVertex(0, EarthSprite.position);
            Spline.SetVertex(1, StarImageSprite.position);
            LinkedDistance.OnDataModified.Register((packet) => ParallaxAnimationUtility.OnDistanceUpdate(packet, this));
        }

        public void SetParallaxFactor(float factor) {
            ParallaxFactor = factor;
        }
        public float GetParallaxFactor() {
            return ParallaxFactor;
        }

        #endregion // Inspector

    }

}