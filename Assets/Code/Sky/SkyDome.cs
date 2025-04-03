using System;
using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using FieldDay.Components;
using FieldDay.SharedState;
using UnityEngine;

namespace Astro {
    public sealed class SkyDome : SharedStateComponent {
        public Transform StarRoot;
        public Transform HorizonRoot;

        [HideInInspector] public Vector3 Position;
        public float Radius = 1000;

        [NonSerialized] public CelestialObject[] AllObjects;
        [NonSerialized] public RingBuffer<CelestialObject> AboveHorizon = new RingBuffer<CelestialObject>(128, RingBufferMode.Expand);

        private void Awake() {
            Position = transform.position;
        }
    }

    public static class SkyDomeUtility {
        static public void FilterByHorizon(SkyDome dome) {
            dome.AboveHorizon.Clear();

            Vector3 up = dome.HorizonRoot.up;

            foreach(var obj in dome.AllObjects) {
                Vector3 pos = obj.transform.localPosition;
                Vector3 dir = pos.normalized;

                float dot = Vector3.Dot(dir, up);
                if (dot < 0.05f) {
                    continue;
                }

                dome.AboveHorizon.PushBack(obj);
            }
        }
    }
}
